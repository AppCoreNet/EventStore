namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class BeginUpdateSubscriptionResult
{
    public int? Id { get; set; }

    public string? SubscriptionId { get; set; }

    public long? Position { get; set; }

    public string? StreamId { get; set; }
}