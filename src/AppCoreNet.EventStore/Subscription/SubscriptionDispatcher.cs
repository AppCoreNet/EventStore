using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppCoreNet.EventStore.Subscription;

public class SubscriptionDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SubscriptionDispatcher(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IServiceProvider serviceProvider = scope.ServiceProvider;

            await using (scope.ConfigureAwait(false))
            {
                try
                {
                    await InvokeSubscriptionsAsync(
                        serviceProvider.GetRequiredService<IEventStore>(),
                        serviceProvider.GetRequiredService<ISubscriptionStore>(),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task InvokeSubscriptionsAsync(
        IEventStore store,
        ISubscriptionStore subscriptionStore,
        CancellationToken cancellationToken)
    {
        WatchSubscriptionResult? watchResult = null;
        ITransaction? transaction = null;

        if (subscriptionStore is ITransactionalStore transactionalSubscriptionStore)
        {
            transaction = await transactionalSubscriptionStore.BeginTransactionAsync(cancellationToken);
        }

        do
        {
            watchResult = await subscriptionStore.WatchAsync(TimeSpan.FromMinutes(1), cancellationToken)
                                                 .ConfigureAwait(false);

            if (watchResult != null)
            {
                IReadOnlyCollection<IEventEnvelope> events =
                    await store.ReadAsync(
                                   watchResult.StreamId,
                                   watchResult.Position,
                                   maxCount: 1024,
                                   cancellationToken: cancellationToken)
                               .ConfigureAwait(false);

                long lastPosition = watchResult.Position;
                foreach (IEventEnvelope @event in events)
                {
                    try
                    {
                        // TODO: invoke event handler
                        lastPosition = watchResult.Position;
                    }
                    catch
                    {
                        break;
                    }
                }

                await subscriptionStore.UpdateAsync(watchResult.SubscriptionId, lastPosition, cancellationToken)
                                       .ConfigureAwait(false);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken)
                                     .ConfigureAwait(false);
                }
            }
        }
        while (watchResult == null);
    }
}