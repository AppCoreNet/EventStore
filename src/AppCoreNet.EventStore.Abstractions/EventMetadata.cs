// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents the metadata of an event.
/// </summary>
public sealed class EventMetadata
{
    // TODO: EventId ?

    /// <summary>
    /// Gets the index of the event.
    /// </summary>
    public long Index { get; init; }

    /// <summary>
    /// Gets the global sequence of the event.
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// Gets the creation time of the event.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets the dictionary of additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }
}