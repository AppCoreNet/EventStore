// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class WatchEventsStoredProcedure : SqlStoredProcedure<Model.WatchEventsResult>
{
    private const string ProcedureName = "WatchEvents";

    required public StreamId StreamId { get; init; }

    required public StreamPosition FromPosition { get; init; }

    required public TimeSpan PollInterval { get; init; }

    required public TimeSpan Timeout { get; init; }

    public WatchEventsStoredProcedure(DbContext dbContext, string? schema)
        : base(dbContext, $"[{SchemaUtils.GetEventStoreSchema(schema)}].{ProcedureName}")
    {
    }

    protected override async Task<Model.WatchEventsResult> ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        int? timeout = DbContext.Database.GetCommandTimeout();
        DbContext.Database.SetCommandTimeout(int.MaxValue);
        try
        {
            return await base.ExecuteCoreAsync(cancellationToken);
        }
        finally
        {
            DbContext.Database.SetCommandTimeout(timeout);
        }
    }

    public static string GetCreateScript(string? schema)
    {
        schema ??= SchemaUtils.GetEventStoreSchema(schema);

        return $"""
                CREATE PROCEDURE [{schema}].{ProcedureName} (
                    @{nameof(StreamId)} NVARCHAR({Constants.StreamIdMaxLength}),
                    @{nameof(FromPosition)} BIGINT,
                    @{nameof(PollInterval)} INT,
                    @{nameof(Timeout)} INT
                    )
                AS
                BEGIN
                    DECLARE @StreamPosition AS BIGINT;
                    DECLARE @WaitTime AS VARCHAR(12) = CONVERT(VARCHAR(12), DATEADD(ms, @{nameof(PollInterval)}, 0), 114);

                    IF @{nameof(FromPosition)} = -2
                    BEGIN
                        IF @{nameof(StreamId)} = '{Constants.StreamIdAll}'
                        BEGIN
                            SELECT TOP 1 @{nameof(FromPosition)} = [{nameof(Model.EventStream.Sequence)}]
                                FROM [{schema}].[{nameof(Model.EventStream)}]
                                ORDER BY [Sequence] DESC;
                        END
                        ELSE
                        BEGIN
                            SELECT TOP 1 @{nameof(FromPosition)} = [{nameof(Model.EventStream.Index)}]
                                FROM [{schema}].[{nameof(Model.EventStream)}]
                                WHERE [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)};
                        END
                        IF @{nameof(FromPosition)} IS NULL SET @{nameof(FromPosition)} = -1;
                    END

                    WHILE @StreamPosition IS NULL
                    BEGIN
                        IF @{nameof(StreamId)} = '{Constants.StreamIdAll}'
                        BEGIN
                            SELECT TOP 1 @StreamPosition = [{nameof(Model.EventStream.Sequence)}]
                                FROM [{schema}].[{nameof(Model.EventStream)}]
                                WHERE [{nameof(Model.EventStream.Sequence)}] > @{nameof(FromPosition)}
                                ORDER BY [{nameof(Model.EventStream.Sequence)}] DESC;
                        END
                        ELSE
                        BEGIN
                            SELECT TOP 1 @StreamPosition = [{nameof(Model.EventStream.Index)}]
                                FROM [{schema}].[{nameof(Model.EventStream)}]
                                WHERE StreamId = @{nameof(StreamId)} AND [{nameof(Model.EventStream.Index)}] > @{nameof(FromPosition)};
                        END

                        IF @StreamPosition IS NULL
                        BEGIN
                            WAITFOR DELAY @WaitTime;
                            SET @{nameof(Timeout)} = @{nameof(Timeout)} - @{nameof(PollInterval)};
                            IF @{nameof(Timeout)} <= 0 BREAK;
                        END
                    END

                    SELECT @StreamPosition AS [{nameof(Model.WatchEventsResult.Position)}];
                END
                """;
    }

    public static string GetDropScript(string? schema)
    {
        return $"DROP PROCEDURE [{schema}].{ProcedureName}";
    }

    protected override SqlParameter[] GetParameters()
    {
        return
        [
            new SqlParameter($"@{nameof(StreamId)}", StreamId.Value),
            new SqlParameter($"@{nameof(FromPosition)}", FromPosition.Value),
            new SqlParameter($"@{nameof(PollInterval)}", (int)PollInterval.TotalMilliseconds),
            new SqlParameter($"@{nameof(Timeout)}", (int)Timeout.TotalMilliseconds),
        ];
    }
}