// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class DeleteStreamCommand : SqlCommand<int>
{
    private readonly string _schema;

    public required StreamId StreamId { get; init; }

    public DeleteStreamCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    private string GetCommandText()
    {
        return $"""
                DECLARE @Result INT;

                IF EXISTS (SELECT 1 FROM [{_schema}].[{nameof(Model.EventStream)}] WHERE [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)})
                BEGIN
                    IF EXISTS (SELECT 1 FROM [{_schema}].[{nameof(Model.EventStream)}] WHERE [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)} AND [{nameof(Model.EventStream.Deleted)}] != 0)
                    BEGIN
                        SET @Result = ${DeleteStreamResultCode.StreamAlreadyDeleted};
                    END
                    ELSE
                    BEGIN
                        UPDATE [{_schema}].[{nameof(Model.EventStream)}]
                        SET [{nameof(Model.EventStream.Deleted)}] = 1
                        WHERE [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)};
                        SET @Result = ${DeleteStreamResultCode.Success};
                    END
                END
                ELSE
                BEGIN
                    SET @Result = ${DeleteStreamResultCode.StreamNotFound};
                END

                SELECT @Result;
                """;
    }

    private SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter($"@{nameof(StreamId)}", StreamId.Value),
        ];
    }

    protected override async Task<int> ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        IQueryable<int> query = DbContext.Database.SqlQueryRaw<int>(GetCommandText(), GetCommandParameters());
        return await query.AsAsyncEnumerable()
                          .FirstAsync(cancellationToken);
    }
}