using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
        Ensure.Arg.NotNull(dataProvider);
        Ensure.Arg.NotNull(serializer);

        _serializer = serializer;
        _dataProvider = dataProvider;
        _dbContext = _dataProvider.DbContext;
    }

    private async Task<T> ExecuteQueryAsync<T>(
        Func<CancellationToken, Task<T>> queryAction,
        bool useTransaction = false,
        CancellationToken cancellationToken = default)
    {
        ITransactionManager transactionManager = _dataProvider.TransactionManager;
        ITransaction? transaction = null;

        try
        {
            if (useTransaction && transactionManager.CurrentTransaction == null)
            {
                transaction = await transactionManager
                                    .BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                                    .ConfigureAwait(false);
            }

            T result;
            await using (transaction)
            {
                result = await queryAction(cancellationToken)
                    .ConfigureAwait(false);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken)
                                     .ConfigureAwait(false);
                }
            }

            return result;
        }
        catch (SqlException error)
        {
            throw new EventStoreException($"An error occured accessing the event store: {error.Message}", error);
        }
    }

    [SuppressMessage(
        "ReSharper",
        "CoVariantArrayConversion",
        Justification = "Parameters are only read.")]
    private async Task<T> ExecuteStoredProcedureAsync<T>(
        string procedureName,
        SqlParameter[] parameters,
        bool useTransaction = false,
        CancellationToken cancellationToken = default)
        where T : class
    {
        string parametersNames = string.Join(", ", parameters.Select(p => p.ParameterName));
        string sql = $"EXEC {procedureName} {parametersNames}";

        IQueryable<T> queryable =
            _dbContext.Set<T>()
                      .FromSqlRaw(sql, parameters)
                      .AsNoTracking();

        return await ExecuteQueryAsync(
                async ct => await queryable.AsAsyncEnumerable()
                                           .FirstAsync(ct)
                                           .ConfigureAwait(false),
                useTransaction,
                cancellationToken)
            .ConfigureAwait(false);
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
                @event.EventType,
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

        Model.WriteResult result = await ExecuteStoredProcedureAsync<Model.WriteResult>(
                Scripts.GetInsertEventsStoredProcedureName(_dbContext.Model),
                parameters,
                true,
                cancellationToken)
            .ConfigureAwait(false);

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
                    events = events.OrderBy(e => e.Position);
                }
                else if (position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Position);
                    maxCount = 1;
                }
                else
                {
                    events = events
                             .OrderBy(e => e.Position)
                             .Where(e => e.Position >= position.Value);
                }

                break;
            }

            case StreamReadDirection.Backward:
            {
                if (position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Position);
                    maxCount = 1;
                }
                else if (position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Position);
                }
                else
                {
                    events = events
                             .OrderByDescending(e => e.Position)
                             .Where(e => e.Position <= position.Value);
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        var streamEvents = await ExecuteQueryAsync(
                async ct => await _dbContext.Set<Model.EventStream>()
                                            .AsNoTracking()
                                            .Where(s => s.StreamId == streamId)
                                            .Select(
                                                s =>
                                                    new
                                                    {
                                                        Events = events.Where(e => e.EventStreamId == s.Id)
                                                                       .Take(maxCount) // do not move Take() before Where()
                                                                       .ToList(),
                                                    })
                                            .FirstOrDefaultAsync(ct)
                                            .ConfigureAwait(false),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (streamEvents == null)
        {
            throw new EventStreamNotFoundException(streamId);
        }

        var result = new List<IEventEnvelope>(maxCount);
        foreach (Model.Event e in streamEvents.Events)
        {
            result.Add(
                new EventEnvelope(
                    e.EventType,
                    _serializer.Deserialize(e.EventType, e.Data) !,
                    new EventMetadata
                    {
                        StreamPosition = e.Position,
                        GlobalPosition = e.Sequence,
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
        string streamId,
        StreamPosition position,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(streamId);

        SqlParameter[] parameters =
        [
            new SqlParameter("@StreamId", streamId),
            new SqlParameter("@FromPosition", position.Value),
            new SqlParameter("@PollInterval", 125),
            new SqlParameter("@Timeout", (int)timeout.TotalMilliseconds),
        ];

        Model.WatchResult result = await ExecuteStoredProcedureAsync<Model.WatchResult>(
                Scripts.GetWatchEventsStoredProcedureName(_dbContext.Model),
                parameters,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.Position.HasValue
            ? new WatchResult((long)result.Position)
            : null;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string streamId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(streamId);

        string sql;
        SqlParameter[] parameters;

        if (streamId == "$all")
        {
            sql = Scripts.DeleteAllStreams(_dbContext.Model);
            parameters = Array.Empty<SqlParameter>();
        }
        else
        {
            sql = Scripts.DeleteStream(_dbContext.Model);
            parameters =
            [
                new SqlParameter("@StreamId", streamId)
            ];
        }

        await ExecuteQueryAsync(
                async ct =>
                    await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters, ct)
                                    .ConfigureAwait(false),
                true,
                cancellationToken)
            .ConfigureAwait(false);
    }
}