namespace AppCoreNet.EventStore;

public interface IEventEnvelope
{
    string EventTypeName { get; }

    object Data { get; }

    EventMetadata Metadata { get; }
}