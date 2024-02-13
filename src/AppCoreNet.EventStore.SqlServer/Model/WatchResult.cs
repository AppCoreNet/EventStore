namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class WatchResult
{
    public string? StreamId { get; init; }

    public long? Sequence { get; set; }

    public long? Position { get; set; }
}