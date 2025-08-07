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

internal sealed class WriteEventsStoredProcedure : SqlStoredProcedure<Model.WriteEventsResult>
{
    private const string ProcedureName = "WriteEvents";

    private readonly string? _schema;
    private readonly IEventStoreSerializer _serializer;

    public required StreamId StreamId { get; init; }

    public required long ExpectedPosition { get; init; }

    public required IEnumerable<object> Events { get; init; }

    public required string LockResource { get; init; }

    public WriteEventsStoredProcedure(DbContext dbContext, string? schema, IEventStoreSerializer serializer)
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
                 DECLARE @StreamDeleted AS INT;
                 DECLARE @LockResult AS INT;

                 IF @{nameof(StreamId)} is NULL RAISERROR('The value for parameter ''{nameof(StreamId)}'' must not be NULL', 16, 1)

                 EXEC @LockResult = sp_getapplock @Resource = @{nameof(LockResource)}, @LockMode = 'Exclusive';
                 IF @LockResult < 0 RAISERROR('Event write lock could not be acquired', 16, 1)

                 SELECT
                    @StreamKey = Id,
                    @StreamSequence = [{nameof(Model.EventStream.Sequence)}],
                    @StreamIndex = [{nameof(Model.EventStream.Index)}],
                    @StreamDeleted = [{nameof(Model.EventStream.Deleted)}]
                 FROM
                    [{schema}].[{nameof(Model.EventStream)}] WITH (UPDLOCK, ROWLOCK)
                 WHERE
                    [{nameof(Model.EventStream.StreamId)}] = @{nameof(StreamId)};

                 IF @StreamIndex IS NOT NULL AND @StreamDeleted != 0
                 BEGIN
                    SELECT
                        {WriteEventsResultCode.StreamDeleted} AS [{nameof(Model.WriteEventsResult.ResultCode)}],
                        @StreamSequence AS [{nameof(Model.WriteEventsResult.Sequence)}],
                        @StreamIndex AS [{nameof(Model.WriteEventsResult.Index)}];
                 END

                 IF @{nameof(ExpectedPosition)} = -2
                 BEGIN
                     IF @StreamIndex IS NOT NULL
                     BEGIN
                         SELECT
                            {WriteEventsResultCode.InvalidStreamState} AS [{nameof(Model.WriteEventsResult.ResultCode)}],
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
                            {WriteEventsResultCode.InvalidStreamState} AS [{nameof(Model.WriteEventsResult.ResultCode)}],
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
                    {WriteEventsResultCode.Success} AS [{nameof(Model.WriteEventsResult.ResultCode)}],
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

        IEnumerable<(EventEnvelope Envelope, int Index)> eventEnvelopes = Events.Select(
            (e, index) => (e as EventEnvelope ?? new EventEnvelope(e), Index: index));

        foreach ((EventEnvelope Envelope, int Index) eventEnvelopeWithIndex in eventEnvelopes)
        {
            EventEnvelope envelope = eventEnvelopeWithIndex.Envelope;
            int index = eventEnvelopeWithIndex.Index;

            dataTable.Rows.Add(
            [
                envelope.EventTypeName,
                envelope.Metadata.CreatedAt,
                index,
                _serializer.Serialize(envelope.Data),
                _serializer.Serialize(envelope.Metadata.Data),
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