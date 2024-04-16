// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Represents a handler for events of subscribed streams.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="event">The <see cref="EventEnvelope"/> which should be handled.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleAsync(EventEnvelope @event, CancellationToken cancellationToken);
}