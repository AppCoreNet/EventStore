namespace AppCoreNet.EventStore;

public interface IEventEnvelope
{
    string EventType { get; }

    object Data { get; }

    EventMetadata Metadata { get; }
}