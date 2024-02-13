using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AppCoreNet.EventStore.SqlServer;

internal static class Scripts
{
    private static string GetEventStoreSchema(IModel model)
    {
        return GetEventStoreSchema(
            (string?)model.FindAnnotation(Constants.EventStoreSchemaAnnotation)
                          ?.Value);
    }

    public static string GetEventStoreSchema(string? schema)
    {
        return schema ?? "dbo";
    }

    public static string GetInsertEventsStoredProcedureName(string? schema) =>
        $"[{GetEventStoreSchema(schema)}].{Constants.InsertEventsProcedureName}";

    public static string GetInsertEventsStoredProcedureName(IModel model) =>
        GetInsertEventsStoredProcedureName(GetEventStoreSchema(model));

    public static string GetWatchEventsStoredProcedureName(string? schema) =>
        $"[{GetEventStoreSchema(schema)}].{Constants.WatchEventsProcedureName}";

    public static string GetWatchEventsStoredProcedureName(IModel model) =>
        GetWatchEventsStoredProcedureName(GetEventStoreSchema(model));

    public static string GetEventTableTypeName(string? schema) =>
        $"[{GetEventStoreSchema(schema)}].{Constants.EventTableTypeName}";

    public static string GetEventTableTypeName(IModel model) =>
        GetEventTableTypeName(GetEventStoreSchema(model));

    public static string CreateEventTableType(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
            CREATE TYPE [{schema}].{Constants.EventTableTypeName} AS TABLE (
                EventType NVARCHAR({Constants.EventTypeMaxLength}),
                CreatedAt DATETIMEOFFSET,
                Offset INT,
                Data NVARCHAR(MAX),
                Metadata NVARCHAR(MAX)
            );
            """;
    }

    public static string CreateInsertEventsProcedure(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
            CREATE PROCEDURE [{schema}].{Constants.InsertEventsProcedureName} (
                @StreamId NVARCHAR({Constants.StreamIdMaxLength}),
                @ExpectedPosition BIGINT,
                @Events [{schema}].{Constants.EventTableTypeName} READONLY
                )
            AS
            BEGIN
                DECLARE @StreamKey AS INT;
                DECLARE @StreamSequence AS BIGINT;
                DECLARE @StreamPosition AS BIGINT;

                IF @StreamId is NULL RAISERROR('The value for parameter ''StreamId'' should not be NULL', 16, 1)

                SELECT @StreamKey = Id, @StreamSequence = [Sequence], @StreamPosition = Position
                    FROM [{schema}].EventStream WITH (UPDLOCK, ROWLOCK)
                    WHERE StreamId = @StreamId;

                IF @ExpectedPosition = -2
                BEGIN
                    IF @StreamPosition IS NOT NULL
                    BEGIN
                        SELECT -1 AS StatusCode, @StreamSequence AS [Sequence], @StreamPosition AS Position;
                        RETURN;
                    END
                END
                ELSE
                BEGIN
                    IF @ExpectedPosition != -1 AND ISNULL(@StreamPosition,-1) != @ExpectedPosition
                    BEGIN
                        SELECT -1 AS StatusCode, @StreamSequence AS [Sequence], @StreamPosition AS Position;
                        RETURN;
                    END
                END

                IF @StreamPosition IS NULL
                BEGIN
                    INSERT INTO [{schema}].EventStream (StreamId, [Sequence], Position) VALUES (@StreamId, 0, 0);
                    SET @StreamKey = SCOPE_IDENTITY();
                    SET @StreamPosition = -1;
                END

                INSERT INTO [{schema}].Event (EventStreamId, EventType, CreatedAt, Position, Data, Metadata)
                    SELECT @StreamKey, EventType, ISNULL(CreatedAt,SYSUTCDATETIME()), @StreamPosition + Offset + 1, Data, Metadata
                        FROM @Events;

                SET @StreamPosition = @StreamPosition + @@ROWCOUNT;
                SET @StreamSequence = SCOPE_IDENTITY();

                UPDATE [{schema}].EventStream
                    SET [Sequence] = @StreamSequence, Position = @StreamPosition
                    WHERE Id = @StreamKey;

                SELECT 0 AS StatusCode, @StreamSequence AS [Sequence], @StreamPosition AS Position;
            END
            """;
    }

    public static string CreateWatchEventsProcedure(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
                CREATE PROCEDURE [{schema}].{Constants.WatchEventsProcedureName} (
                    @FromSequence BIGINT,
                    @PollInterval INT,
                    @Timeout INT
                    )
                AS
                BEGIN
                    DECLARE @StreamId AS NVARCHAR({Constants.StreamIdMaxLength})
                    DECLARE @StreamSequence AS BIGINT;
                    DECLARE @StreamPosition AS BIGINT;
                    DECLARE @WaitTime AS VARCHAR(12) = CONVERT(VARCHAR(12), DATEADD(ms, @PollInterval, 0), 114);

                    WHILE @StreamId IS NULL
                    BEGIN
                        SELECT TOP 1 @StreamId = StreamId, @StreamSequence = [Sequence], @StreamPosition = Position
                            FROM [{schema}].EventStream
                            WHERE [Sequence] > @FromSequence
                            ORDER BY [Sequence];

                        IF @StreamId IS NULL
                        BEGIN
                            WAITFOR DELAY @WaitTime;
                            SET @Timeout = @Timeout - @PollInterval;
                            IF @Timeout <= 0 BREAK
                        END
                    END

                    SELECT @StreamId AS StreamId, @StreamSequence AS [Sequence], @StreamPosition AS Position;
                END
                """;
    }

    public static string DropEventTableType(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
            BEGIN
                DROP TYPE [{schema}].{Constants.EventTableTypeName};
            END
            """;
    }

    public static string DropInsertEventsProcedure(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
            BEGIN
                DROP PROCEDURE [{schema}].{Constants.InsertEventsProcedureName};
            END
            """;
    }

    public static string DropWatchEventsProcedure(string? schema)
    {
        schema ??= GetEventStoreSchema(schema);

        return $"""
                BEGIN
                    DROP PROCEDURE [{schema}].{Constants.WatchEventsProcedureName};
                END
                """;
    }

    public static string DeleteStream(string? schema, string streamId) =>
        $"DELETE FROM [{GetEventStoreSchema(schema)}].{nameof(Model.EventStream)} WHERE StreamId=@StreamId";

    public static string DeleteStream(IModel model, string streamId) =>
        DeleteStream(GetEventStoreSchema(model), streamId);

    public static string DeleteAllStreams(string? schema) =>
        $"DELETE FROM [{GetEventStoreSchema(schema)}].{nameof(Model.EventStream)}";

    public static string DeleteAllStreams(IModel model) =>
        DeleteAllStreams(GetEventStoreSchema(model));
}