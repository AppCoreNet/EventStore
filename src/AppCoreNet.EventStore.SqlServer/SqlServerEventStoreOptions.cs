// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore.SqlServer;

/// <summary>
/// Provides options for the <see cref="SqlServerEventStore{TDbContext}"/>.
/// </summary>
public class SqlServerEventStoreOptions
{
    /// <summary>
    /// Gets or sets the poll interval used when watching for events.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(125);

    /// <summary>
    /// Gets or sets the name of the event store database schema.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the schema defaults to <c>dbo</c>.
    /// </remarks>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    /// <remarks>
    /// The application name is used to acquire database locks to reduce polling when watching subscriptions.
    /// </remarks>
    public string? ApplicationName { get; set; }
}