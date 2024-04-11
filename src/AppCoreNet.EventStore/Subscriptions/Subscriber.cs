// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

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

    internal Subscriber(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        SubscriptionId = subscriptionId;
        StreamId = streamId;
        ListenerFactory = listenerFactory;
    }
}
