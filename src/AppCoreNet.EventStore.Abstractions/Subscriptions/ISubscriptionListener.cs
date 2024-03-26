// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Represents a listener for subscriptions.
/// </summary>
public interface ISubscriptionListener
{
    /// <summary>
    /// Invoked to handle an event.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleAsync(
        SubscriptionId subscriptionId,
        EventEnvelope @event,
        CancellationToken cancellationToken = default);
}