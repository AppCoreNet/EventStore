using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.SqlServer;

public sealed class SqlServerSubscriptionManager<TDbContext> : ISubscriptionManager
    where TDbContext : DbContext
{
    private readonly DbContextDataProvider<TDbContext> _dataProvider;
    private readonly SqlServerEventStore<TDbContext> _eventStore;
    private readonly TDbContext _dbContext;
    private readonly SqlServerEventStoreOptions _options;

    public SqlServerSubscriptionManager(
        DbContextDataProvider<TDbContext> dataProvider,
        SqlServerEventStore<TDbContext> eventStore,
        IOptionsMonitor<SqlServerEventStoreOptions> optionsMonitor)
    {
        Ensure.Arg.NotNull(dataProvider);
        Ensure.Arg.NotNull(eventStore);
        Ensure.Arg.NotNull(optionsMonitor);

        _dataProvider = dataProvider;
        _eventStore = eventStore;
        _dbContext = _dataProvider.DbContext;
        _options = optionsMonitor.CurrentValue;
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        string id,
        StreamId streamId,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(id);
        Ensure.Arg.NotNull(streamId);

        var command = new CreateSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            SubscriptionId = id,
            StreamId = streamId.Value,
        };

        await command.ExecuteAsync(cancellationToken)
                     .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(id);

        var command = new DeleteSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            SubscriptionId = id,
        };

        await command.ExecuteAsync(cancellationToken)
                     .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WatchSubscriptionsResult?> WatchAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        await using ITransaction transaction =
            await _dataProvider.TransactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                               .ConfigureAwait(false);

        var procedure = new WatchSubscriptionsStoredProcedure(_dbContext, _options.SchemaName)
        {
            PollInterval = _options.PollInterval,
            Timeout = timeout,
        };

        Model.WatchSubscriptionsResult result =
            await procedure.ExecuteAsync(cancellationToken)
                                .ConfigureAwait(false);

        return result.Id == null
            ? null
            : new WatchSubscriptionsResult(result.SubscriptionId!, result.StreamId!, (long)result.Position!);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(
        string subscriptionId,
        Func<StreamId, long, CancellationToken, Task<long>> callback,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotEmpty(subscriptionId);
        Ensure.Arg.NotNull(callback);

        await using ITransaction transaction =
            await _dataProvider.TransactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                               .ConfigureAwait(false);

        var procedure = new BeginUpdateSubscriptionStoredProcedure(_dbContext, _options.SchemaName)
        {
            SubscriptionId = subscriptionId,
        };

        Model.BeginUpdateSubscriptionResult result =
            await procedure.ExecuteAsync(cancellationToken)
                           .ConfigureAwait(false);

        if (result.Id == null)
            return;

        long position = await callback(result.StreamId!, (long)result.Position!, cancellationToken)
            .ConfigureAwait(false);

        var updateCommand = new UpdateSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            Id = (int)result.Id,
            Position = position,
        };

        await updateCommand.ExecuteAsync(cancellationToken)
                           .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken)
                         .ConfigureAwait(false);
    }

    internal async Task ProcessAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await using ITransaction transaction =
            await _dataProvider.TransactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                               .ConfigureAwait(false);

        var watchProcedure = new WatchSubscriptionsStoredProcedure(_dbContext, _options.SchemaName)
        {
            PollInterval = _options.PollInterval,
            Timeout = timeout,
        };

        Model.WatchSubscriptionsResult result =
            await watchProcedure.ExecuteAsync(cancellationToken)
                           .ConfigureAwait(false);

        if (result.Id == null)
            return;

        var streamId = new StreamId(result.StreamId!);
        long position = (long)result.Position!;

        IReadOnlyCollection<IEventEnvelope> events =
            await _eventStore.ReadAsync(
                                 streamId,
                                 position,
                                 maxCount: 64,
                                 cancellationToken: cancellationToken)
                             .ConfigureAwait(false);

        long? lastPosition = null;
        ExceptionDispatchInfo? exceptionDispatchInfo = null;

        try
        {
            foreach (IEventEnvelope @event in events)
            {
                lastPosition = streamId.IsWildcard
                    ? @event.Metadata.GlobalPosition
                    : @event.Metadata.StreamPosition;
            }
        }
        catch (Exception error)
        {
            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(error);
        }

        if (lastPosition != null)
        {
            var updateCommand = new UpdateSubscriptionCommand(_dbContext, _options.SchemaName)
            {
                Id = (int)result.Id,
                Position = (long)lastPosition,
            };

            await updateCommand.ExecuteAsync(cancellationToken)
                               .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken)
                             .ConfigureAwait(false);
        }

        exceptionDispatchInfo?.Throw();
    }
}