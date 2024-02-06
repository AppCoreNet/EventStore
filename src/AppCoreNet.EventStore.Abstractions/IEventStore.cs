// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents a store for events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Writes events to the store.
    /// </summary>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="events">The <see cref="IEnumerable{T}"/> of events to write.</param>
    /// <param name="expectedVersion">The expected version after writing the last event.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAsync(
        string streamId,
        IEnumerable<IEventEnvelope> events,
        long? expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from the store.
    /// </summary>
    /// <remarks>
    /// The number of events read may be less than the requested count if fewer events are available.
    /// </remarks>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="fromVersion">The version of the first event to read.</param>
    /// <param name="maxCount">The maximum number of events to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    Task<IReadOnlyCollection<IEventEnvelope>> ReadAsync(
        string streamId,
        long? fromVersion,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task WatchAsync(long? fromOffset, CancellationToken cancellationToken = default);
}