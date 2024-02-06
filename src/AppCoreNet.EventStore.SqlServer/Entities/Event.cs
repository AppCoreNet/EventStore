using System;
using System.Collections.Generic;

namespace AppCoreNet.EventStore.SqlServer.Entities;

public class Event
{
    public int EventStreamId { get; }

    public long Sequence { get; }

    public string Data { get; }

    public string EventType { get; }

    public DateTimeOffset CreatedAt { get;  }

    public List<EventMetadata> Metadata { get; }

    public Event(
        int eventStreamId,
        long sequence,
        string data,
        string eventType,
        DateTimeOffset createdAt,
        List<EventMetadata> metadata)
    {
        EventStreamId = eventStreamId;
        Sequence = sequence;
        Data = data;
        EventType = eventType;
        CreatedAt = createdAt;
        Metadata = metadata;
    }
}