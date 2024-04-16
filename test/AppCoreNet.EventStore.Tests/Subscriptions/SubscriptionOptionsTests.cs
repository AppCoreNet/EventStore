// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.Subscriptions;

public class SubscriptionOptionsTests
{
    [Fact]
    public void CannotAddDuplicateSubscription()
    {
        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        var options = new SubscriptionOptions();
        options.AddListener(subscriptionId, streamId, Substitute.For<ISubscriptionListener>());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                options.AddListener(subscriptionId, streamId, Substitute.For<ISubscriptionListener>());
            });
    }

    [Fact]
    public void CannotAddDuplicateEventHandlerSubscription()
    {
        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        var options = new SubscriptionOptions();
        options.AddListener(subscriptionId, streamId, Substitute.For<ISubscriptionListener>());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                options.AddEventHandlers(subscriptionId, streamId, o => { });
            });
    }

    [Fact]
    public void ReturnsSubscribersFromRegisteredListeners()
    {
        var subscriptionId1 = SubscriptionId.NewId();
        StreamId streamId1 = StreamId.All;
        var listener1 = Substitute.For<Func<IServiceProvider, ISubscriptionListener>>();

        var subscriptionId2 = SubscriptionId.NewId();
        StreamId streamId2 = StreamId.Prefix("order.");
        var listener2 = Substitute.For<Func<IServiceProvider, ISubscriptionListener>>();

        var options = new SubscriptionOptions();
        options.AddListener(subscriptionId1, streamId1, listener1);
        options.AddListener(subscriptionId2, streamId2, listener2);

        IEnumerable<Subscriber> subscribers = options.GetSubscribers();

        subscribers.Should()
                   .BeEquivalentTo(
                       new[]
                       {
                           new Subscriber(subscriptionId1, streamId1, listener1),
                           new Subscriber(subscriptionId2, streamId2, listener2),
                       });
    }

    [Fact]
    public void AddEventHandlersRegistersListener()
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
                o.Add(eventHandler1);
            });

        options.AddEventHandlers(
            subscriptionId,
            streamId,
            o =>
            {
                o.Add(eventHandler2);
            });

        IEnumerable<Subscriber> subscribers = options.GetSubscribers()
                                                     .ToArray();

        subscribers.Should()
                   .HaveCount(1);

        Subscriber subscriber = subscribers.First();
        subscriber.SubscriptionId.Should()
                  .Be(subscriptionId);
        subscriber.StreamId.Should()
                  .Be(streamId);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceProvider))
                       .Returns(serviceProvider);

        serviceProvider.GetService(typeof(IOptions<SubscriptionOptions>))
                       .Returns(Options.Create(options));

        ISubscriptionListener listener = subscriber.ListenerFactory(serviceProvider);

        listener.Should()
                .BeOfType<EventHandlerSubscriptionListener>();
    }
}