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
    /// <summary>
    /// Creates a subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="streamId">The ID (can be wildcard) of the stream to subscribe to.</param>
    /// <param name="failIfExists">Whether to throw <see cref="EventStoreException"/> if the subscription does already exist.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(
        SubscriptionId subscriptionId,
        StreamId streamId,
        bool failIfExists = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID (can be wildcard) of the subscription.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="SubscriptionNotFoundException">The subscription was not found.</exception>
    Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for new events with subscriptions.
    /// </summary>
    /// <param name="timeout">Specifies how long to wait for new events to be available.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The last observed subscription; <c>null</c> if timeout has elapsed.</returns>
    Task<WatchSubscriptionResult?> WatchAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the event position of a subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="position">The new position of the subscription.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(SubscriptionId subscriptionId, long position, CancellationToken cancellationToken = default);
}