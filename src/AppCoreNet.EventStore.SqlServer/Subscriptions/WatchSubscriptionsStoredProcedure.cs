// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscriptions;

internal sealed class WatchSubscriptionsStoredProcedure : SqlStoredProcedure<Model.WatchSubscriptionsResult>
{
    private const string ProcedureName = "WatchSubscriptions";

    required public TimeSpan PollInterval { get; init; }

    required public TimeSpan Timeout { get; init; }

    required public string LockResource { get; init; }

    public WatchSubscriptionsStoredProcedure(DbContext dbContext, string? schema)
        : base(dbContext, $"[{SchemaUtils.GetEventStoreSchema(schema)}].{ProcedureName}")
    {
    }

    protected override async Task<Model.WatchSubscriptionsResult> ExecuteCoreAsync(CancellationToken cancellationToken)
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
                    @{nameof(PollInterval)} INT,
                    @{nameof(Timeout)} INT,
                    @{nameof(LockResource)} NVARCHAR(max)
                    )
                AS
                BEGIN
                    DECLARE @Id AS INT;
                    DECLARE @SubscriptionId AS NVARCHAR({Constants.SubscriptionIdMaxLength});
                    DECLARE @StreamId AS NVARCHAR({Constants.StreamIdMaxLength});
                    DECLARE @Position AS BIGINT;
                    DECLARE @WaitTime AS VARCHAR(12) = CONVERT(VARCHAR(12), DATEADD(ms, @PollInterval, 0), 114);
                    DECLARE @LockTime AS DATETIME;
                    DECLARE @LockResult AS INT;

                    SELECT @LockTime = CURRENT_TIMESTAMP;
                    EXEC @LockResult = sp_getapplock @Resource = @{nameof(LockResource)}, @LockMode = 'Exclusive', @LockTimeout = @Timeout;
                    SET @Timeout = @Timeout - DATEDIFF(MILLISECOND, @LockTime, CURRENT_TIMESTAMP);

                    IF @LockResult >= 0
                    BEGIN
                        WHILE @Id IS NULL
                        BEGIN
                            SELECT TOP 1
                                @Id = SU.[{nameof(Model.EventSubscription.Id)}],
                                @SubscriptionId = SU.[{nameof(Model.EventSubscription.SubscriptionId)}],
                                @Position = SU.[{nameof(Model.EventSubscription.Position)}],
                                @StreamId = SU.[{nameof(Model.EventSubscription.StreamId)}]
                            FROM
                                [{schema}].[{nameof(Model.EventSubscription)}] AS SU WITH (UPDLOCK, ROWLOCK, READPAST),
                                [{schema}].[{nameof(Model.EventStream)}] AS ST
                            WHERE
                                (ST.[{nameof(Model.EventStream.StreamId)}] = SU.[{nameof(Model.EventSubscription.StreamId)}]
                                    AND SU.[{nameof(Model.EventSubscription.Position)}] < ST.[{nameof(Model.EventStream.Index)}])
                                OR (SU.[{nameof(Model.EventSubscription.StreamId)}] = '{Constants.StreamIdAll}'
                                    AND SU.[{nameof(Model.EventSubscription.Position)}] < ST.[{nameof(Model.EventStream.Sequence)}])
                                OR (LEFT(SU.[{nameof(Model.EventSubscription.StreamId)}], 1) = '*'
                                    AND ST.[{nameof(Model.EventStream.StreamId)}] LIKE '%' + RIGHT(SU.[{nameof(Model.EventSubscription.StreamId)}], LEN(SU.[{nameof(Model.EventSubscription.StreamId)}])-1)
                                    AND SU.[{nameof(Model.EventSubscription.Position)}] < ST.[{nameof(Model.EventStream.Sequence)}])
                                OR (RIGHT(SU.[{nameof(Model.EventSubscription.StreamId)}], 1) = '*'
                                    AND ST.[{nameof(Model.EventStream.StreamId)}] LIKE LEFT(SU.[{nameof(Model.EventSubscription.StreamId)}], LEN(SU.[{nameof(Model.EventSubscription.StreamId)}])-1) + '%'
                                    AND SU.[{nameof(Model.EventSubscription.Position)}] < ST.[{nameof(Model.EventStream.Sequence)}])
                            ORDER BY
                                SU.[{nameof(Model.EventSubscription.ProcessedAt)}],
                                ST.[{nameof(Model.EventStream.Sequence)}]
                                DESC;

                            IF @Id IS NULL
                            BEGIN
                                WAITFOR DELAY @WaitTime;
                                SET @{nameof(Timeout)} = @{nameof(Timeout)} - @{nameof(PollInterval)};
                                IF @{nameof(Timeout)} <= 0 BREAK;
                            END
                        END

                        EXEC sp_releaseapplock @Resource = @{nameof(LockResource)}
                    END

                    SELECT
                        @Id AS [{nameof(Model.WatchSubscriptionsResult.Id)}],
                        @SubscriptionId AS [{nameof(Model.WatchSubscriptionsResult.SubscriptionId)}],
                        @Position AS [{nameof(Model.WatchSubscriptionsResult.Position)}],
                        @StreamId AS [{nameof(Model.WatchSubscriptionsResult.StreamId)}];
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
            new SqlParameter($"@{nameof(PollInterval)}", (int)PollInterval.TotalMilliseconds),
            new SqlParameter($"@{nameof(Timeout)}", (int)Timeout.TotalMilliseconds),
            new SqlParameter($"@{nameof(LockResource)}", LockResource),
        ];
    }
}