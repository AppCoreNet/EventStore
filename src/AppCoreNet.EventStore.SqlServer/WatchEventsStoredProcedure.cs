using System;
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

    public static string GetCreateScript(string? schema)
    {
        schema ??= SchemaUtils.GetEventStoreSchema(schema);

        return $"""
                CREATE PROCEDURE [{schema}].{ProcedureName} (
                    @StreamId NVARCHAR({Constants.StreamIdMaxLength}),
                    @FromPosition BIGINT,
                    @PollInterval INT,
                    @Timeout INT
                    )
                AS
                BEGIN
                    DECLARE @StreamPosition AS BIGINT;
                    DECLARE @WaitTime AS VARCHAR(12) = CONVERT(VARCHAR(12), DATEADD(ms, @PollInterval, 0), 114);

                    IF @FromPosition = -2
                    BEGIN
                        IF @StreamId = '{Constants.StreamIdAll}'
                        BEGIN
                            SELECT TOP 1 @FromPosition = [Sequence]
                                FROM [{schema}].EventStream
                                ORDER BY [Sequence] DESC;
                        END
                        ELSE
                        BEGIN
                            SELECT TOP 1 @FromPosition = Position
                                FROM [{schema}].EventStream
                                WHERE StreamId = @StreamId;
                        END
                        IF @FromPosition IS NULL SET @FromPosition = -1;
                    END

                    WHILE @StreamPosition IS NULL
                    BEGIN
                        IF @StreamId = '{Constants.StreamIdAll}'
                        BEGIN
                            SELECT TOP 1 @StreamPosition = [Sequence]
                                FROM [{schema}].EventStream
                                WHERE [Sequence] > @FromPosition
                                ORDER BY [Sequence] DESC;
                        END
                        ELSE
                        BEGIN
                            SELECT TOP 1 @StreamPosition = Position
                                FROM [{schema}].EventStream
                                WHERE StreamId = @StreamId AND [Position] > @FromPosition;
                        END

                        IF @StreamPosition IS NULL
                        BEGIN
                            WAITFOR DELAY @WaitTime;
                            SET @Timeout = @Timeout - @PollInterval;
                            IF @Timeout <= 0 BREAK;
                        END
                    END

                    SELECT @StreamPosition AS Position;
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
            new SqlParameter("@StreamId", StreamId.Value),
            new SqlParameter("@FromPosition", FromPosition.Value),
            new SqlParameter("@PollInterval", (int)PollInterval.TotalMilliseconds),
            new SqlParameter("@Timeout", (int)Timeout.TotalMilliseconds),
        ];
    }
}