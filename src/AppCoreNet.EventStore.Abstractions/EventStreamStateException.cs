using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Exception which is thrown when an event stream was not in the expected state.
/// </summary>
public class EventStreamStateException : EventStoreException
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
    /// Initializes a new instance of the <see cref="EventStreamStateException"/> class.
    /// </summary>
    /// <param name="streamId">The ID of the event stream.</param>
    /// <param name="expectedState">The state of the stream which was expected.</param>
    public EventStreamStateException(string streamId, StreamState expectedState)
        : base($"Event stream '{streamId}' was not in the expected state '{expectedState}'.")
    {
        Ensure.Arg.NotEmpty(streamId);

        StreamId = streamId;
        ExpectedState = expectedState;
    }
}