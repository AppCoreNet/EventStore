namespace AppCoreNet.EventStore.SqlServer.Entities;

public class EventStream
{
    public int Id { get; }

    public string StreamId { get; }

    public long Version { get; }

    public EventStream(int id, string streamId, long version)
    {
        Id = id;
        StreamId = streamId;
        Version = version;
    }
}