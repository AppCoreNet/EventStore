// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Watches subscriptions and invokes registered listeners.
/// </summary>
public sealed class SubscriptionService : BackgroundService
{
    private readonly SubscriptionManager _subscriptionManager;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
    /// </summary>
    /// <param name="subscriptionManager">The <see cref="SubscriptionManager"/> used resolve listeners.</param>
    /// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/>.</param>
    public SubscriptionService(SubscriptionManager subscriptionManager, IServiceScopeFactory scopeFactory)
    {
        Ensure.Arg.NotNull(subscriptionManager);
        Ensure.Arg.NotNull(scopeFactory);

        _subscriptionManager = subscriptionManager;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriptionManager.InitializeAsync(cancellationToken);

        await base.StartAsync(cancellationToken)
                  .ConfigureAwait(false);
    }

    /// <inheritdoc />
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
                    await ProcessAsync(
                        serviceProvider.GetRequiredService<IEventStore>(),
                        serviceProvider.GetRequiredService<ISubscriptionStore>(),
                        serviceProvider,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessAsync(
        IEventStore store,
        ISubscriptionStore subscriptionStore,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        WatchSubscriptionResult? watchResult;

        do
        {
            ITransaction? transaction = null;
            if (subscriptionStore is ITransactionalStore transactionalSubscriptionStore)
            {
                transaction = await transactionalSubscriptionStore.BeginTransactionAsync(cancellationToken)
                                                                  .ConfigureAwait(false);
            }

            await using (transaction)
            {
                watchResult = await subscriptionStore.WatchAsync(TimeSpan.FromMinutes(1), cancellationToken)
                                                     .ConfigureAwait(false);

                if (watchResult != null)
                {
                    IReadOnlyCollection<EventEnvelope> events =
                        await store.ReadAsync(
                                       watchResult.StreamId,
                                       watchResult.Position.Value + 1,
                                       maxCount: 1024,
                                       cancellationToken: cancellationToken)
                                   .ConfigureAwait(false);

                    ISubscriptionListener listener = _subscriptionManager.CreateListener(
                        watchResult.SubscriptionId,
                        serviceProvider);

                    long lastPosition = watchResult.Position.Value;
                    foreach (EventEnvelope @event in events)
                    {
                        try
                        {
                            await listener.HandleAsync(watchResult.SubscriptionId, @event, cancellationToken)
                                          .ConfigureAwait(false);

                            lastPosition = watchResult.StreamId.IsWildcard
                                ? @event.Metadata.Sequence
                                : @event.Metadata.Index;
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

                        await transaction.DisposeAsync()
                                         .ConfigureAwait(false);
                    }
                }
            }
        }
        while (watchResult == null);
    }
}