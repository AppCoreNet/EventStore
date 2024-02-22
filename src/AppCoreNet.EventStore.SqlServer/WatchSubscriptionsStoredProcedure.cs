using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class WatchSubscriptionsStoredProcedure : SqlStoredProcedure<Model.WatchSubscriptionsResult>
{
    private const string ProcedureName = "WatchSubscriptions";

    required public TimeSpan PollInterval { get; init; }

    required public TimeSpan Timeout { get; init; }

    public WatchSubscriptionsStoredProcedure(DbContext dbContext, string? schema)
        : base(dbContext, $"[{SchemaUtils.GetEventStoreSchema(schema)}].{ProcedureName}")
    {
    }

    public static string GetCreateScript(string? schema)
    {
        schema ??= SchemaUtils.GetEventStoreSchema(schema);

        return $"""
                CREATE PROCEDURE [{schema}].{ProcedureName} (
                    @PollInterval INT,
                    @Timeout INT
                    )
                AS
                BEGIN
                    DECLARE @Id AS INT;
                    DECLARE @SubscriptionId AS NVARCHAR({Constants.SubscriptionIdMaxLength});
                    DECLARE @StreamId AS NVARCHAR({Constants.StreamIdMaxLength});
                    DECLARE @Position AS BIGINT;
                    DECLARE @WaitTime AS VARCHAR(12) = CONVERT(VARCHAR(12), DATEADD(ms, @PollInterval, 0), 114);

                    WHILE @Id IS NULL
                    BEGIN
                        SELECT TOP 1 @Id = SU.Id, @SubscriptionId = SU.SubscriptionId, @Position = SU.Position, @StreamId = SU.StreamId
                            FROM [events].EventSubscription AS SU WITH (READPAST), [events].EventStream AS ST
                            WHERE (ST.StreamId = SU.StreamId AND SU.Position <= ST.Position)
                                OR (SU.StreamId = '{Constants.StreamIdAll}' AND SU.Position <= ST.[Sequence])
                                OR (LEFT(SU.StreamId, 1) = '*' AND ST.StreamId LIKE '%' + RIGHT(SU.StreamId, LEN(SU.StreamId)-1) AND SU.Position <= ST.[Sequence])
                                OR (RIGHT(SU.StreamId, 1) = '*' AND ST.StreamId LIKE LEFT(SU.StreamId, LEN(SU.StreamId)-1) + '%' AND SU.Position <= ST.[Sequence])
                            ORDER BY SU.ProcessedAt, ST.[Sequence] DESC;

                        IF @Id IS NULL
                        BEGIN
                            WAITFOR DELAY @WaitTime;
                            SET @Timeout = @Timeout - @PollInterval;
                            IF @Timeout <= 0 BREAK;
                        END
                    END

                    SELECT @Id AS Id, @SubscriptionId AS SubscriptionId, @Position AS Position, @StreamId AS StreamId;
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
            new SqlParameter("@PollInterval", (int)PollInterval.TotalMilliseconds),
            new SqlParameter("@Timeout", (int)Timeout.TotalMilliseconds),
        ];
    }
}