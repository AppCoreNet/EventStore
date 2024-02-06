namespace AppCoreNet.EventStore.SqlServer.Entities;

public sealed class EventMetadata
{
    public string Name { get; }

    public string Value { get; }

    public EventMetadata(string name, string value)
    {
        Name = name;
        Value = value;
    }
}