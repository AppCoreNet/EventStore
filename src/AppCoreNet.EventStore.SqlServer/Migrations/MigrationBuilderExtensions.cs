using Microsoft.EntityFrameworkCore.Migrations;

namespace AppCoreNet.EventStore.SqlServer.Migrations;

public static class MigrationBuilderExtensions
{
    private static void CreateStoredProcedure(this MigrationBuilder builder, string sql)
    {
        builder.Sql($"EXEC('{sql.Replace("'", "''")}')");
    }

    public static void CreateEventStoreProcedures(this MigrationBuilder builder, string? schema = null)
    {
        schema ??= Scripts.GetEventStoreSchema(schema);

        builder.CreateStoredProcedure(Scripts.CreateEventTableType(schema));
        builder.CreateStoredProcedure(Scripts.CreateInsertEventsProcedure(schema));
        builder.CreateStoredProcedure(Scripts.CreateWatchEventsProcedure(schema));
    }

    public static void DropEventStoreProcedures(this MigrationBuilder builder, string? schema = null)
    {
        builder.Sql(Scripts.DropWatchEventsProcedure(schema));
        builder.Sql(Scripts.DropInsertEventsProcedure(schema));
        builder.Sql(Scripts.DropEventTableType(schema));
    }
}