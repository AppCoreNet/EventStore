// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class Event
{
    public long Sequence { get; init; }

    public int EventStreamId { get; init; }

    public EventStream? EventStream { get; init; }

    public required string EventType { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public long Index { get; init; }

    public required string Data { get; init; }

    public string? Metadata { get; init; }
}