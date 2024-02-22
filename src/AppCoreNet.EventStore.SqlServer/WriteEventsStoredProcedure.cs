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
    private const string EventTableTypeName = "EventTable";

    private readonly string? _schema;
    private readonly IEventStoreSerializer _serializer;

    required public string StreamId { get; init; }

    required public long ExpectedPosition { get; init; }

    required public IEnumerable<object> Events { get; init; }

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
             CREATE TYPE [{schema}].{EventTableTypeName} AS TABLE (
                 EventType NVARCHAR({Constants.EventTypeMaxLength}),
                 CreatedAt DATETIMEOFFSET,
                 Offset INT,
                 Data NVARCHAR(MAX),
                 Metadata NVARCHAR(MAX)
             );
             """;

        yield return
            $"""
             CREATE PROCEDURE [{schema}].{ProcedureName} (
                 @StreamId NVARCHAR({Constants.StreamIdMaxLength}),
                 @ExpectedPosition BIGINT,
                 @Events [{schema}].{EventTableTypeName} READONLY
                 )
             AS
             BEGIN
                 DECLARE @StreamKey AS INT;
                 DECLARE @StreamSequence AS BIGINT;
                 DECLARE @StreamPosition AS BIGINT;

                 IF @StreamId is NULL RAISERROR('The value for parameter ''StreamId'' must not be NULL', 16, 1)

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

    public static IEnumerable<string> GetDropScripts(string? schema)
    {
        yield return $"DROP PROCEDURE [{schema}].{ProcedureName}";
        yield return $"DROP TYPE [{schema}].{EventTableTypeName}";
    }

    private DataTable CreateEventDataTable()
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn("EventType", typeof(string)));
        dataTable.Columns.Add(new DataColumn("CreatedAt", typeof(DateTimeOffset)));
        dataTable.Columns.Add(new DataColumn("Offset", typeof(int)));
        dataTable.Columns.Add(new DataColumn("Data", typeof(string)));
        dataTable.Columns.Add(new DataColumn("Metadata", typeof(string)));

        IEnumerable<(IEventEnvelope, int index)> eventEnvelopes = Events.Select(
            (e, index) => (e as IEventEnvelope ?? new EventEnvelope(e), index));

        foreach ((IEventEnvelope @event, int index) in eventEnvelopes)
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
            new SqlParameter("@StreamId", StreamId),
            new SqlParameter("@ExpectedPosition", ExpectedPosition),
            new SqlParameter("@Events", SqlDbType.Structured)
            {
                TypeName = $"[{_schema}].{EventTableTypeName}",
                Value = CreateEventDataTable(),
            },
        ];
    }
}