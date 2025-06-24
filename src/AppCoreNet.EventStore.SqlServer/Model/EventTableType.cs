// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore.SqlServer.Model;

/// <summary>
/// Used to pass events to the <see cref="WriteEventsStoredProcedure"/>. This class
/// does not represent a database table.
/// </summary>
internal sealed class EventTableType
{
    public required string EventType { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public int Offset { get; init; }

    public required string Data { get; init; }

    public string? Metadata { get; set; }
}