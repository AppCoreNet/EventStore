using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public class EventStreamNotFoundException : EventStoreException
{
    public string StreamId { get; }

    public EventStreamNotFoundException(string streamId)
        : base($"Event stream '{streamId}' not found.")
    {
        Ensure.Arg.NotEmpty(streamId);
        StreamId = streamId;
    }
}