// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.Subscriptions;

internal sealed class EventHandlerSubscriptionListener : ISubscriptionListener
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriptionOptions _options;

    public EventHandlerSubscriptionListener(IServiceProvider serviceProvider, IOptions<SubscriptionOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async Task HandleAsync(SubscriptionId subscriptionId, EventEnvelope @event, CancellationToken cancellationToken = default)
    {
        if (!_options.EventHandlerOptions.TryGetValue(subscriptionId, out EventHandlerSubscriptionOptions? options))
            return;

        foreach (Func<IServiceProvider, IEventHandler> handlerFactory in options.EventHandlerFactories)
        {
            IEventHandler handler = handlerFactory(_serviceProvider);

            await handler.HandleAsync(@event, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}