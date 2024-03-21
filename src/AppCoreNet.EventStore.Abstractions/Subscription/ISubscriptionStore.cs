// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.Subscription;

/// <summary>
/// Represents the event subscription store.
/// </summary>
public interface ISubscriptionStore
{
    Task CreateAsync(
        SubscriptionId subscriptionId,
        StreamId streamId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);

    Task<WatchSubscriptionResult?> WatchAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    Task UpdateAsync(SubscriptionId subscriptionId, long position, CancellationToken cancellationToken = default);
}