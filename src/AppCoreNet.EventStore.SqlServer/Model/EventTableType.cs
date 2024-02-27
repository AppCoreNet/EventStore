using System;

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class EventTableType
{
    required public string EventType { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public int Offset { get; init; }

    required public string Data { get; init; }

    public string? Metadata { get; set; }
}