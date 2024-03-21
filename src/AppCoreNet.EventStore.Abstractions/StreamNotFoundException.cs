using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public class StreamNotFoundException : EventStoreException
{
    public string StreamId { get; }

    public StreamNotFoundException(string streamId)
        : base($"Event stream '{streamId}' not found.")
    {
        Ensure.Arg.NotEmpty(streamId);
        StreamId = streamId;
    }
}