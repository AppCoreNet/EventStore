using Microsoft.EntityFrameworkCore.Migrations;

namespace AppCoreNet.EventStore.SqlServer.Migrations;

public static class MigrationBuilderExtensions
{
    private static void ExecuteSql(this MigrationBuilder builder, string sql)
    {
        builder.Sql($"EXEC('{sql.Replace("'", "''")}')");
    }

    public static void CreateEventStore(this MigrationBuilder builder, string? schema = null)
    {
        foreach (string script in WriteEventsSqlStoredProcedure.GetCreateScripts(schema))
        {
            builder.ExecuteSql(script);
        }

        builder.ExecuteSql(WatchEventsStoredProcedure.GetCreateScript(schema));
        builder.ExecuteSql(WatchSubscriptionsStoredProcedure.GetCreateScript(schema));
    }

    public static void DropEventStore(this MigrationBuilder builder, string? schema = null)
    {
        foreach (string script in WriteEventsSqlStoredProcedure.GetDropScripts(schema))
        {
            builder.ExecuteSql(script);
        }

        builder.ExecuteSql(WatchEventsStoredProcedure.GetDropScript(schema));
        builder.ExecuteSql(WatchSubscriptionsStoredProcedure.GetDropScript(schema));
    }
}