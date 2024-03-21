// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents a listener for events.
/// </summary>
public interface IEventListener
{
    /// <summary>
    /// Invoked to handle an event.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleAsync(EventEnvelope @event, CancellationToken cancellationToken = default);
}