namespace AppCoreNet.EventStore;

public sealed class EventEnvelope<T> : IEventEnvelope
    where T : notnull
{
    public T Data { get; }

    object IEventEnvelope.Data => Data;

    public EventMetadata Metadata { get; }

    public EventEnvelope(T data, EventMetadata metadata)
    {
        Data = data;
        Metadata = metadata;
    }
}