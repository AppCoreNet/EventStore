using System;
using System.Collections.Generic;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public sealed class EventMetadata
{
    public string EventType { get; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public string? TraceId { get; init; }

    public IReadOnlyDictionary<string, object>? Data { get; init; }

    public EventMetadata(string eventType)
    {
        Ensure.Arg.NotEmpty(eventType);
        EventType = eventType;
    }
}