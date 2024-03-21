﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore.SqlServer.Model;

/// <summary>
/// Used to pass events to the <see cref="WriteEventsSqlStoredProcedure"/>. This class
/// does not represent a database table.
/// </summary>
internal sealed class EventTableType
{
    required public string EventType { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public int Offset { get; init; }

    required public string Data { get; init; }

    public string? Metadata { get; set; }
}