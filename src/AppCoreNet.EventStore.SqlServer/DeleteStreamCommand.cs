// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class DeleteStreamCommand : SqlTextCommand
{
    private readonly string _schema;

    public required StreamId StreamId { get; init; }

    public DeleteStreamCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        if (StreamId == StreamId.All)
        {
            return $"DELETE FROM [{_schema}].[{nameof(Model.EventStream)}]";
        }

        if (StreamId.IsPrefix)
        {
            return $"DELETE FROM [{_schema}].[{nameof(Model.EventStream)}]"
                   + $" WHERE [{nameof(Model.EventStream.StreamId)}] LIKE @{nameof(StreamId)} + '%'";
        }

        if (StreamId.IsSuffix)
        {
            return $"DELETE FROM [{_schema}].[{nameof(Model.EventStream)}]"
                   + $" WHERE [{nameof(Model.EventStream.StreamId)}] LIKE '%' + @{nameof(StreamId)}";
        }

        return $"DELETE FROM [{_schema}].[{nameof(Model.EventStream)}]"
               + $" WHERE [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)}";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        if (StreamId == StreamId.All)
            return Array.Empty<SqlParameter>();

        return
        [
            new SqlParameter($"@{nameof(StreamId)}", StreamId.Value.Trim('*'))
        ];
    }
}