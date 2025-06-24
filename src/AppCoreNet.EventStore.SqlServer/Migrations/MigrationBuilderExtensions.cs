// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.SqlServer.Subscriptions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AppCoreNet.EventStore.SqlServer.Migrations;

/// <summary>
/// Provides SQL server event store related extensions for <see cref="MigrationBuilder"/>.
/// </summary>
public static class MigrationBuilderExtensions
{
    private static void ExecuteSql(this MigrationBuilder builder, string sql)
    {
        builder.Sql($"EXEC('{sql.Replace("'", "''")}')");
    }

    /// <summary>
    /// Creates the stored procedures used by the event store.
    /// </summary>
    /// <param name="builder">The <see cref="MigrationBuilder"/>.</param>
    /// <param name="schema">The name of the database schema.</param>
    public static void CreateEventStore(this MigrationBuilder builder, string? schema = null)
    {
        Ensure.Arg.NotNull(builder);

        foreach (string script in WriteEventsStoredProcedure.GetCreateScripts(schema))
        {
            builder.ExecuteSql(script);
        }

        builder.ExecuteSql(WatchEventsStoredProcedure.GetCreateScript(schema));
        builder.ExecuteSql(WatchSubscriptionsStoredProcedure.GetCreateScript(schema));
    }

    /// <summary>
    /// Drops the stored procedures used by the event store.
    /// </summary>
    /// <param name="builder">The <see cref="MigrationBuilder"/>.</param>
    /// <param name="schema">The name of the database schema.</param>
    public static void DropEventStore(this MigrationBuilder builder, string? schema = null)
    {
        Ensure.Arg.NotNull(builder);

        foreach (string script in WriteEventsStoredProcedure.GetDropScripts(schema))
        {
            builder.ExecuteSql(script);
        }

        builder.ExecuteSql(WatchEventsStoredProcedure.GetDropScript(schema));
        builder.ExecuteSql(WatchSubscriptionsStoredProcedure.GetDropScript(schema));
    }
}