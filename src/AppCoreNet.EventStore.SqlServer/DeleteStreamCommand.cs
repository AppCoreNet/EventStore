using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class DeleteStreamCommand : SqlTextCommand
{
    private readonly string _schema;

    required public string StreamId { get; init; }

    public DeleteStreamCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        // TODO: add support for StreamId wildcards
        if (StreamId != "$all")
        {
            return $"DELETE FROM [{_schema}].{nameof(Model.EventStream)} WHERE StreamId=@StreamId";
        }

        return $"DELETE FROM [{_schema}].{nameof(Model.EventStream)}";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        if (StreamId != "$all")
        {
            return
            [
                new SqlParameter("@StreamId", StreamId)
            ];
        }

        return Array.Empty<SqlParameter>();
    }
}