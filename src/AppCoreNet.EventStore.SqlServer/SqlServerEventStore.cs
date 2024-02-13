using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

public class SqlServerEventStore<TDbContext> : IEventStore
    where TDbContext : DbContext
{
    private readonly IEventStoreSerializer _serializer;
    private readonly DbContextDataProvider<TDbContext> _dataProvider;
    private readonly TDbContext _dbContext;

    public SqlServerEventStore(DbContextDataProvider<TDbContext> dataProvider, IEventStoreSerializer serializer)
    {
        _serializer = serializer;
        _dataProvider = dataProvider;
        _dbContext = _dataProvider.DbContext;
    }

    private async Task<T> ExecuteStoredProcedureAsync<T>(
        string procedureName,
        SqlParameter[] parameters,
        CancellationToken cancellationToken)
        where T : class
    {
        string parametersNames = string.Join(", ", parameters.Select(p => p.ParameterName));
        string sql = $"EXEC {procedureName} {parametersNames}";

        IAsyncEnumerable<T> resultSet =
            _dbContext.Set<T>()
                     .FromSqlRaw(sql, parameters)
                     .AsNoTracking()
                     .AsAsyncEnumerable();

        return await resultSet.FirstAsync(cancellationToken);
    }

    private DataTable CreateEventDataTable(IEnumerable<object> events)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(Model.Event.EventType), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.Event.CreatedAt), typeof(DateTimeOffset)));
        dataTable.Columns.Add(new DataColumn("Offset", typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.Event.Data), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(Model.Event.Metadata), typeof(string)));

        foreach ((IEventEnvelope @event, int index) in events.Select(
                     (e, index) => (e as IEventEnvelope ?? new EventEnvelope(e), index)))
        {
            dataTable.Rows.Add(
            [
                @event.Metadata.EventType,
                @event.Metadata.CreatedAt,
                index,
                _serializer.Serialize(@event.Data),
                _serializer.Serialize(@event.Metadata.Data),
            ]);
        }

        return dataTable;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        string streamId,
        IEnumerable<object> events,
        StreamState state,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(streamId);
        Ensure.Arg.NotNull(events);

        ITransactionManager transactionManager = _dataProvider.TransactionManager;
        ITransaction? transaction = null;

        if (transactionManager.CurrentTransaction == null)
        {
            transaction = await transactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                                                  .ConfigureAwait(false);
        }

        await using (transaction)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@StreamId", streamId),
                new SqlParameter("@ExpectedPosition", state.Value),
                new SqlParameter("@Events", SqlDbType.Structured)
                {
                    TypeName = Scripts.GetEventTableTypeName(_dbContext.Model),
                    Value = CreateEventDataTable(events),
                },
            ];

            var result = await ExecuteStoredProcedureAsync<Model.WriteResult>(
                Scripts.GetInsertEventsStoredProcedureName(_dbContext.Model),
                parameters,
                cancellationToken);

            switch (result.StatusCode)
            {
                case 0:
                    break;
                case -1:
                    throw new EventStreamStateException(streamId, state);
                default:
                    throw new NotImplementedException(
                        $"Unknown status code '{result.StatusCode}' returned from stored procedure");
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<IEventEnvelope>> ReadAsync(
        string streamId,
        StreamPosition position,
        StreamReadDirection direction = StreamReadDirection.Forward,
        int maxCount = 1,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(streamId);
        Ensure.Arg.InRange(maxCount, 0, int.MaxValue);

        IQueryable<Model.Event> events =
            _dbContext.Set<Model.Event>()
                      .AsNoTracking();

        switch (direction)
        {
            case StreamReadDirection.Forward:
            {
                if (position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Position)
                                   .Take(maxCount);
                }
                else if (position == StreamPosition.End)
                {
                    events = events
                             .OrderByDescending(e => e.Position)
                             .Take(1);
                }
                else
                {
                    events = events
                             .OrderBy(e => e.Position)
                             .Where(e => e.Position >= position.Value)
                             .Take(maxCount);
                }

                break;
            }

            case StreamReadDirection.Backward:
            {
                if (position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Position)
                                   .Take(1);
                }
                else if (position == StreamPosition.End)
                {
                    events = events
                             .OrderByDescending(e => e.Position)
                             .Take(maxCount);
                }
                else
                {
                    events = events
                             .OrderByDescending(e => e.Position)
                             .Where(e => e.Position <= position.Value)
                             .Take(maxCount);
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        var streamEvents =
            await _dbContext.Set<Model.EventStream>()
                            .AsNoTracking()
                            .Where(s => s.StreamId == streamId)
                            .Select(
                                s =>
                                    new
                                    {
                                        Events = events.Where(e => e.EventStreamId == s.Id)
                                                       .ToList(),
                                    })
                            .FirstOrDefaultAsync(cancellationToken);

        if (streamEvents == null)
        {
            throw new EventStreamNotFoundException(streamId);
        }

        var result = new List<IEventEnvelope>(maxCount);
        foreach (Model.Event e in streamEvents.Events)
        {
            result.Add(
                new EventEnvelope(
                    _serializer.Deserialize(e.EventType, e.Data) !,
                    new EventMetadata(e.EventType)
                    {
                        Position = e.Position,
                        CreatedAt = e.CreatedAt,
                        Data = (Dictionary<string, string>?)_serializer.Deserialize(
                            Constants.StringDictionaryTypeName,
                            e.Metadata),
                    }));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<WatchResult?> WatchAsync(
        string? continuationToken,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(
                continuationToken ?? "-1",
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long fromSequence))
        {
            throw new ArgumentException(
                $"Argument value '{continuationToken}' is not in the correct format.",
                nameof(continuationToken));
        }

        SqlParameter[] parameters =
        [
            new SqlParameter("@FromSequence", fromSequence),
            new SqlParameter("@PollInterval", 125),
            new SqlParameter("@Timeout", (int)timeout.TotalMilliseconds),
        ];

        var result = await ExecuteStoredProcedureAsync<Model.WatchResult>(
            Scripts.GetWatchEventsStoredProcedureName(_dbContext.Model),
            parameters,
            cancellationToken);

        if (result.StreamId == null)
        {
            // timeout without any new events
            return null;
        }

        return new WatchResult(
            result.StreamId,
            (long)result.Position!,
            Convert.ToString(result.Sequence, CultureInfo.InvariantCulture) !);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string streamId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(streamId);

        ITransactionManager transactionManager = _dataProvider.TransactionManager;
        ITransaction? transaction = null;

        if (transactionManager.CurrentTransaction == null)
        {
            transaction = await transactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                                                  .ConfigureAwait(false);
        }

        await using (transaction)
        {
            string sql;
            SqlParameter[] parameters = Array.Empty<SqlParameter>();

            if (streamId == "$all")
            {
                sql = Scripts.DeleteAllStreams(_dbContext.Model);
            }
            else
            {
                sql = Scripts.DeleteStream(_dbContext.Model, streamId);
                parameters =
                [
                    new SqlParameter("@StreamId", streamId)
                ];
            }

            await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken)
                            .ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }
    }
}