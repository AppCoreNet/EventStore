using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public sealed class EventEnvelope : IEventEnvelope
{
    public object Data { get; }

    public EventMetadata Metadata { get; }

    public EventEnvelope(object data)
    {
        Ensure.Arg.NotNull(data);

        Data = data;
        Metadata = new EventMetadata(data.GetType().FullName!);
    }

    public EventEnvelope(object data, EventMetadata metadata)
    {
        Ensure.Arg.NotNull(data);
        Ensure.Arg.NotNull(metadata);

        Data = data;
        Metadata = metadata;
    }
}