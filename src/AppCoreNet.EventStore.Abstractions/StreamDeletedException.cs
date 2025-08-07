// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Exception which is thrown when a event stream was deleted.
/// </summary>
public class StreamDeletedException : EventStoreException
{
    /// <summary>
    /// Gets the stream ID.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamDeletedException"/> class.
    /// </summary>
    /// <param name="streamId">The stream ID which was not found.</param>
    public StreamDeletedException(string streamId)
        : base($"Event stream '{streamId}' is deleted.")
    {
        Ensure.Arg.NotEmpty(streamId);
        StreamId = streamId;
    }
}