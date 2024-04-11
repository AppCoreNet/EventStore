// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Represents a subscriber.
/// </summary>
public sealed class Subscriber
{
    /// <summary>
    /// Gets the subscription ID.
    /// </summary>
    public SubscriptionId SubscriptionId { get; }

    /// <summary>
    /// Gets the subscribed stream ID.
    /// </summary>
    public StreamId StreamId { get; }

    /// <summary>
    /// Gets the factory used to create the subscription listener.
    /// </summary>
    public Func<IServiceProvider, ISubscriptionListener> ListenerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscriber"/> class.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listenerFactory">The factory for the <see cref="ISubscriptionListener"/>.</param>
    public Subscriber(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotNull(listenerFactory);

        SubscriptionId = subscriptionId;
        StreamId = streamId;
        ListenerFactory = listenerFactory;
    }
}
