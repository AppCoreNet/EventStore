// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.SqlServer;

public sealed class SqlServerEventStore<TDbContext> : IEventStore
    where TDbContext : DbContext
{
    private readonly IEventStoreSerializer _serializer;
    private readonly DbContextDataProvider<TDbContext> _dataProvider;
    private readonly TDbContext _dbContext;
    private readonly SqlServerEventStoreOptions _options;

    public SqlServerEventStore(
        DbContextDataProvider<TDbContext> dataProvider,
        IEventStoreSerializer serializer,
        IOptionsMonitor<SqlServerEventStoreOptions> optionsMonitor)
    {
        Ensure.Arg.NotNull(dataProvider);
        Ensure.Arg.NotNull(serializer);
        Ensure.Arg.NotNull(optionsMonitor);

        _serializer = serializer;
        _dataProvider = dataProvider;
        _dbContext = _dataProvider.DbContext;
        _options = optionsMonitor.CurrentValue;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        StreamId streamId,
        IEnumerable<object> events,
        StreamState state,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotWildcard(streamId);
        Ensure.Arg.NotNull(events);

        var procedure = new WriteEventsSqlStoredProcedure(_dbContext, _options.SchemaName, _serializer)
        {
            StreamId = streamId,
            ExpectedPosition = state.Value,
            Events = events,
        };

        Model.WriteEventsResult result =
            await _dataProvider.TransactionManager.ExecuteAsync(
                                   async ct =>
                                       await procedure.ExecuteAsync(ct)
                                                      .ConfigureAwait(false),
                                   cancellationToken)
                               .ConfigureAwait(false);

        switch (result.StatusCode)
        {
            case 0:
                break;
            case -1:
                throw new StreamStateException(streamId.Value, state);
            default:
                throw new NotImplementedException(
                    $"Unknown status code '{result.StatusCode}' returned from stored procedure");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<IEventEnvelope>> ReadAsync(
        StreamId streamId,
        StreamPosition position,
        StreamReadDirection direction = StreamReadDirection.Forward,
        int maxCount = 1,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.InRange(maxCount, 0, int.MaxValue);

        var query = new ReadEventsCommand(_dbContext)
        {
            StreamId = streamId,
            Position = position,
            Direction = direction,
            MaxCount = maxCount,
        };

        IReadOnlyCollection<Model.Event> events = await query.ExecuteAsync(cancellationToken)
                                                             .ConfigureAwait(false);

        var result = new List<IEventEnvelope>(maxCount);
        foreach (Model.Event e in events)
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
    public async Task<WatchEventResult?> WatchAsync(
        StreamId streamId,
        StreamPosition position,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);

        // TODO: watch prefix/suffix streams
        if (streamId.IsPrefix || streamId.IsSuffix)
            throw new NotImplementedException();

        var procedure = new WatchEventsStoredProcedure(_dbContext, _options.SchemaName)
        {
            StreamId = streamId,
            FromPosition = position,
            PollInterval = _options.PollInterval,
            Timeout = timeout,
        };

        Model.WatchEventsResult result =
            await procedure.ExecuteAsync(cancellationToken)
                           .ConfigureAwait(false);

        return result.Position.HasValue
            ? new WatchEventResult((long)result.Position)
            : null;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(StreamId streamId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);

        var command = new DeleteStreamCommand(_dbContext, _options.SchemaName)
        {
            StreamId = streamId,
        };

        int affectedRows = await command.ExecuteAsync(cancellationToken)
                                        .ConfigureAwait(false);

        if (!streamId.IsWildcard && affectedRows == 0)
        {
            throw new StreamNotFoundException(streamId.Value);
        }
    }
}