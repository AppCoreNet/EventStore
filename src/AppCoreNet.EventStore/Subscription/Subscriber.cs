// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore.Subscription;

internal sealed class Subscriber
{
    public StreamId StreamId { get; }

    public Func<IServiceProvider, ISubscriptionListener> ListenerFactory { get; }

    public Subscriber(StreamId streamId, Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        StreamId = streamId;
        ListenerFactory = listenerFactory;
    }
}