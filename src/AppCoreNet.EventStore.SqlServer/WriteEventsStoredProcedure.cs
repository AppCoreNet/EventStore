// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class WriteEventsSqlStoredProcedure : SqlStoredProcedure<Model.WriteEventsResult>
{
    private const string ProcedureName = "WriteEvents";

    private readonly string? _schema;
    private readonly IEventStoreSerializer _serializer;

    required public StreamId StreamId { get; init; }

    required public long ExpectedPosition { get; init; }

    required public IEnumerable<object> Events { get; init; }

    required public string LockResource { get; init; }

    public WriteEventsSqlStoredProcedure(DbContext dbContext, string? schema, IEventStoreSerializer serializer)
        : base(dbContext, $"[{SchemaUtils.GetEventStoreSchema(schema)}].{ProcedureName}")
    {
        _schema = schema;
        _serializer = serializer;
    }

    public static IEnumerable<string> GetCreateScripts(string? schema)
    {
        schema ??= SchemaUtils.GetEventStoreSchema(schema);

        yield return
            $"""
             CREATE TYPE [{schema}].[{nameof(Model.EventTableType)}] AS TABLE (
                 [{nameof(Model.EventTableType.EventType)}] NVARCHAR({Constants.EventTypeMaxLength}),
                 [{nameof(Model.EventTableType.CreatedAt)}] DATETIMEOFFSET,
                 [{nameof(Model.EventTableType.Offset)}] INT,
                 [{nameof(Model.EventTableType.Data)}] NVARCHAR(MAX),
                 [{nameof(Model.EventTableType.Metadata)}] NVARCHAR(MAX)
             );
             """;

        yield return
            $"""
             CREATE PROCEDURE [{schema}].{ProcedureName} (
                 @{nameof(StreamId)} NVARCHAR({Constants.StreamIdMaxLength}),
                 @{nameof(ExpectedPosition)} BIGINT,
                 @{nameof(Events)} [{schema}].[{nameof(Model.EventTableType)}] READONLY,
                 @{nameof(LockResource)} NVARCHAR(max)
                 )
             AS
             BEGIN
                 DECLARE @StreamKey AS INT;
                 DECLARE @StreamSequence AS BIGINT;
                 DECLARE @StreamIndex AS BIGINT;
                 DECLARE @LockResult AS INT;

                 IF @{nameof(StreamId)} is NULL RAISERROR('The value for parameter ''{nameof(StreamId)}'' must not be NULL', 16, 1)

                 EXEC @LockResult = sp_getapplock @Resource = @{nameof(LockResource)}, @LockMode = 'Exclusive';
                 IF @LockResult < 0 RAISERROR('Event write lock could not be acquired', 16, 1)

                 SELECT
                    @StreamKey = Id,
                    @StreamSequence = [{nameof(Model.EventStream.Sequence)}],
                    @StreamIndex = [{nameof(Model.EventStream.Index)}]
                 FROM
                    [{schema}].[{nameof(Model.EventStream)}] WITH (UPDLOCK, ROWLOCK)
                 WHERE
                    [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)};

                 IF @{nameof(ExpectedPosition)} = -2
                 BEGIN
                     IF @StreamIndex IS NOT NULL
                     BEGIN
                         SELECT
                            -1 AS [{nameof(Model.WriteEventsResult.StatusCode)}],
                            @StreamSequence AS [{nameof(Model.WriteEventsResult.Sequence)}],
                            @StreamIndex AS [{nameof(Model.WriteEventsResult.Index)}];
                         RETURN;
                     END
                 END
                 ELSE
                 BEGIN
                     IF @{nameof(ExpectedPosition)} != -1 AND ISNULL(@StreamIndex,-1) != @{nameof(ExpectedPosition)}
                     BEGIN
                         SELECT
                            -1 AS [{nameof(Model.WriteEventsResult.StatusCode)}],
                            @StreamSequence AS [{nameof(Model.WriteEventsResult.Sequence)}],
                            @StreamIndex AS [{nameof(Model.WriteEventsResult.Index)}];
                         RETURN;
                     END
                 END

                 IF @StreamIndex IS NULL
                 BEGIN
                     INSERT INTO
                        [{schema}].[{nameof(Model.EventStream)}] (
                            [{nameof(Model.EventStream.StreamId)}],
                            [{nameof(Model.EventStream.Sequence)}],
                            [{nameof(Model.EventStream.Index)}]
                        )
                        VALUES (
                            @{nameof(StreamId)},
                            0,
                            0
                        );
                     SET @StreamKey = SCOPE_IDENTITY();
                     SET @StreamIndex = -1;
                 END

                 INSERT INTO
                    [{schema}].[{nameof(Model.Event)}] (
                        [{nameof(Model.Event.EventStreamId)}],
                        [{nameof(Model.Event.EventType)}],
                        [{nameof(Model.Event.CreatedAt)}],
                        [{nameof(Model.Event.Index)}],
                        [{nameof(Model.Event.Data)}],
                        [{nameof(Model.Event.Metadata)}]
                    )
                    SELECT
                        @StreamKey,
                        [{nameof(Model.EventTableType.EventType)}],
                        ISNULL([{nameof(Model.EventTableType.CreatedAt)}],SYSUTCDATETIME()),
                        @StreamIndex + [{nameof(Model.EventTableType.Offset)}] + 1,
                        [{nameof(Model.EventTableType.Data)}],
                        [{nameof(Model.EventTableType.Metadata)}]
                    FROM
                        @{nameof(Events)}
                    ORDER BY [{nameof(Model.EventTableType.Offset)}];

                 SET @StreamIndex = @StreamIndex + @@ROWCOUNT;
                 SET @StreamSequence = SCOPE_IDENTITY();

                 UPDATE
                    [{schema}].[{nameof(Model.EventStream)}]
                 SET
                    [{nameof(Model.EventStream.Sequence)}] = @StreamSequence,
                    [{nameof(Model.EventStream.Index)}] = @StreamIndex
                 WHERE
                    Id = @StreamKey;

                 SELECT
                    0 AS [{nameof(Model.WriteEventsResult.StatusCode)}],
                    @StreamSequence AS [{nameof(Model.WriteEventsResult.Sequence)}],
                    @StreamIndex AS [{nameof(Model.WriteEventsResult.Index)}];
             END
             """;
    }

    public static IEnumerable<string> GetDropScripts(string? schema)
    {
        yield return $"DROP PROCEDURE [{schema}].{ProcedureName}";
        yield return $"DROP TYPE [{schema}].[{nameof(Model.EventTableType)}]";
    }

    private DataTable CreateEventDataTable()
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(Model.EventTableType.EventType), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.EventTableType.CreatedAt), typeof(DateTimeOffset)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.EventTableType.Offset), typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.EventTableType.Data), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.EventTableType.Metadata), typeof(string)));

        IEnumerable<(EventEnvelope, int index)> eventEnvelopes = Events.Select(
            (e, index) => (e as EventEnvelope ?? new EventEnvelope(e), index));

        foreach ((EventEnvelope @event, int index) in eventEnvelopes)
        {
            dataTable.Rows.Add(
            [
                @event.EventTypeName,
                @event.Metadata.CreatedAt,
                index,
                _serializer.Serialize(@event.Data),
                _serializer.Serialize(@event.Metadata.Data),
            ]);
        }

        return dataTable;
    }

    protected override SqlParameter[] GetParameters()
    {
        return
        [
            new SqlParameter($"@{nameof(StreamId)}", StreamId.Value),
            new SqlParameter($"@{nameof(ExpectedPosition)}", ExpectedPosition),
            new SqlParameter($"@{nameof(Events)}", SqlDbType.Structured)
            {
                TypeName = $"[{_schema}].[{nameof(Model.EventTableType)}]",
                Value = CreateEventDataTable(),
            },
            new SqlParameter($"@{nameof(LockResource)}", LockResource)
        ];
    }
}