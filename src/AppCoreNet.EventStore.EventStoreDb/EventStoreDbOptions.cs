// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.EventStoreDb;

/// <summary>
/// Provides options for the <see cref="EventStoreDbEventStore"/>.
/// </summary>
public class EventStoreDbOptions
{
    /// <summary>
    /// Gets or sets a prefix for stream names.
    /// </summary>
    public string? StreamNamePrefix { get; set; }
}