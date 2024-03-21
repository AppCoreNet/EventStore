// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Exception which is thrown when an event stream was not in the expected state.
/// </summary>
public class StreamStateException : EventStoreException
{
    /// <summary>
    /// Gets the ID of the stream.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the state of the stream which was expected.
    /// </summary>
    public StreamState ExpectedState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamStateException"/> class.
    /// </summary>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="expectedState">The state of the stream which was expected.</param>
    public StreamStateException(string streamId, StreamState expectedState)
        : base($"Event stream '{streamId}' was not in the expected state '{expectedState}'.")
    {
        Ensure.Arg.NotEmpty(streamId);

        StreamId = streamId;
        ExpectedState = expectedState;
    }
}