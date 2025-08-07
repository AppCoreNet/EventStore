// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.SqlServer;

/// <summary>
/// Provides a <see cref="IEventStore"/> implementation using SQL Server.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public sealed class SqlServerEventStore<TDbContext> : IEventStore
    where TDbContext : DbContext
{
    private readonly IEventStoreSerializer _serializer;
    private readonly DbContextDataProvider<TDbContext> _dataProvider;
    private readonly TDbContext _dbContext;
    private readonly SqlServerEventStoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerEventStore{TDbContext}"/> class.
    /// </summary>
    /// <param name="dataProvider">The data provider used to access the event store.</param>
    /// <param name="serializer">The serializer used to serialize/deserialize events.</param>
    /// <param name="optionsMonitor">The <see cref="IOptionsMonitor{TOptions}"/> used to resolve the <see cref="SqlServerEventStoreOptions"/>.</param>
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

        var procedure = new WriteEventsStoredProcedure(_dbContext, _options.SchemaName, _serializer)
        {
            StreamId = streamId,
            ExpectedPosition = state.Value,
            Events = events,
            LockResource = _options.ApplicationName + "-WriteEvents",
        };

        Model.WriteEventsResult result =
            await _dataProvider.TransactionManager.ExecuteAsync(
                                   async ct =>
                                       await procedure.ExecuteAsync(ct)
                                                      .ConfigureAwait(false),
                                   cancellationToken)
                               .ConfigureAwait(false);

        switch (result.ResultCode)
        {
            case WriteEventsResultCode.Success:
                break;
            case WriteEventsResultCode.InvalidStreamState:
                throw new StreamStateException(streamId.Value, state);
            case WriteEventsResultCode.StreamDeleted:
                throw new StreamDeletedException(streamId.Value);
            default:
                throw new NotImplementedException(
                    $"Unknown status code '{result.ResultCode}' returned from stored procedure");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<EventEnvelope>> ReadAsync(
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

        IReadOnlyCollection<Model.Event> events =
            await query.ExecuteAsync(cancellationToken)
                       .ConfigureAwait(false);

        var result = new List<EventEnvelope>(events.Count);
        foreach (Model.Event e in events)
        {
            result.Add(
                new EventEnvelope(
                    e.EventType,
                    _serializer.Deserialize(e.EventType, e.Data)!,
                    new EventMetadata
                    {
                        Index = e.Index,
                        Sequence = e.Sequence,
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

        switch (result.ResultCode)
        {
            case WatchEventsResultCode.Success:
                break;
            case WatchEventsResultCode.StreamNotFound:
                throw new StreamNotFoundException(streamId.Value);
            case WriteEventsResultCode.StreamDeleted:
                throw new StreamDeletedException(streamId.Value);
            default:
                throw new NotImplementedException(
                    $"Unknown status code '{result.ResultCode}' returned from stored procedure");
        }

        return result.Position.HasValue
            ? new WatchEventResult((long)result.Position)
            : null;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(StreamId streamId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotWildcard(streamId);

        var command = new DeleteStreamCommand(_dbContext, _options.SchemaName)
        {
            StreamId = streamId,
        };

        int result = await command.ExecuteAsync(cancellationToken)
                                  .ConfigureAwait(false);

        switch (result)
        {
            case DeleteStreamResultCode.Success:
                break;
            case DeleteStreamResultCode.StreamNotFound:
                throw new StreamNotFoundException(streamId.Value);
            case DeleteStreamResultCode.StreamAlreadyDeleted:
                throw new StreamDeletedException(streamId.Value);
            default:
                throw new NotImplementedException();
        }
    }
}