// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.Subscriptions;

public class EventHandlerSubscriptionListenerTests
{
    [Fact]
    public async Task InvokesRegisteredHandlers()
    {
        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;
        var eventHandler1 = Substitute.For<IEventHandler>();
        var eventHandler2 = Substitute.For<IEventHandler>();

        var options = new SubscriptionOptions();
        options.AddEventHandlers(
            subscriptionId,
            streamId,
            o =>
            {
                o.Add(eventHandler1)
                 .Add(eventHandler2);
            });

        Subscriber subscriber = options.GetSubscribers().ToArray()[0];

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceProvider))
                       .Returns(serviceProvider);

        serviceProvider.GetService(typeof(IOptions<SubscriptionOptions>))
                       .Returns(Options.Create(options));

        ISubscriptionListener listener = subscriber.ListenerFactory(serviceProvider);

        var @event = new EventEnvelope("order-created", "1");
        await listener.HandleAsync(subscriptionId, @event);

        await eventHandler1.Received()
                           .HandleAsync(@event, Arg.Any<CancellationToken>());

        await eventHandler1.Received()
                           .HandleAsync(@event, Arg.Any<CancellationToken>());
    }
}