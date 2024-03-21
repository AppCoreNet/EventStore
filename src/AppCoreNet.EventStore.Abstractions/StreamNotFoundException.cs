// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Exception which is thrown when a event stream was not found.
/// </summary>
public class StreamNotFoundException : EventStoreException
{
    /// <summary>
    /// Gets the stream ID.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamNotFoundException"/> class.
    /// </summary>
    /// <param name="streamId">The stream ID which was not found.</param>
    public StreamNotFoundException(string streamId)
        : base($"Event stream '{streamId}' not found.")
    {
        Ensure.Arg.NotEmpty(streamId);
        StreamId = streamId;
    }
}