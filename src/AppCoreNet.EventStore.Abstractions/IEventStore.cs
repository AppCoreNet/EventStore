// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
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
    /// <param name="state">The expected state of the stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="EventStreamStateException">The version of the stream was not the expected one.</exception>
    Task WriteAsync(
        string streamId,
        IEnumerable<object> events,
        StreamState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from the store.
    /// </summary>
    /// <remarks>
    /// The number of events read may be less than the requested count if fewer events are available.
    /// </remarks>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="position">The position where to start reading from the stream. Note that the position is inclusive.</param>
    /// <param name="direction">The direction when reading from the stream.</param>
    /// <param name="maxCount">The maximum number of events to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    /// <exception cref="EventStreamNotFoundException">The stream was not found.</exception>
    Task<IReadOnlyCollection<IEventEnvelope>> ReadAsync(
        string streamId,
        StreamPosition position,
        StreamReadDirection direction = StreamReadDirection.Forward,
        int maxCount = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for new events in the store.
    /// </summary>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="position">The last observed position in the event stream.</param>
    /// <param name="timeout">Specifies how long to wait for new events to be available.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The last observed position; <c>null</c> if timeout has elapsed.</returns>
    Task<WatchResult?> WatchAsync(
        string streamId,
        StreamPosition position,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all events from an event stream.
    /// </summary>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(string streamId, CancellationToken cancellationToken = default);
}