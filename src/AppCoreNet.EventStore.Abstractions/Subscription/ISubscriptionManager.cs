// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.Subscription;

/// <summary>
/// Manages subscription listeners.
/// </summary>
public interface ISubscriptionManager
{
    /// <summary>
    /// Creates a subscription and registers the specified listener factory.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="streamId">The ID of the stream to subscribe to.</param>
    /// <param name="listenerFactory">The factory used to create the subscription listener.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the listener and deletes the specified subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);
}