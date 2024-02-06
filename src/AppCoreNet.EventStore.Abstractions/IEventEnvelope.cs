namespace AppCoreNet.EventStore;

public interface IEventEnvelope
{
    object Data { get; }

    EventMetadata Metadata { get; }
}