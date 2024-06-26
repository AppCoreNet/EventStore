﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.SqlServer.Subscriptions;

/// <summary>
/// Provides a <see cref="ISubscriptionStore"/> implementation using SQL Server.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public sealed class SqlServerSubscriptionStore<TDbContext> : ISubscriptionStore, ITransactionalStore
    where TDbContext : DbContext
{
    private readonly DbContextDataProvider<TDbContext> _dataProvider;
    private readonly TDbContext _dbContext;
    private readonly SqlServerEventStoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSubscriptionStore{TDbContext}"/> class.
    /// </summary>
    /// <param name="dataProvider">The data provider used to access the subscription store.</param>
    /// <param name="optionsMonitor">The <see cref="IOptionsMonitor{TOptions}"/> used to resolve the <see cref="SqlServerEventStoreOptions"/>.</param>
    public SqlServerSubscriptionStore(
        DbContextDataProvider<TDbContext> dataProvider,
        IOptionsMonitor<SqlServerEventStoreOptions> optionsMonitor)
    {
        Ensure.Arg.NotNull(dataProvider);
        Ensure.Arg.NotNull(optionsMonitor);

        _dataProvider = dataProvider;
        _dbContext = _dataProvider.DbContext;
        _options = optionsMonitor.CurrentValue;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Subscription> GetAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<Model.EventSubscription> enumerable =
            _dbContext.Set<Model.EventSubscription>()
                      .AsNoTracking()
                      .AsAsyncEnumerable();

        await using ConfiguredCancelableAsyncEnumerable<Model.EventSubscription>.Enumerator enumerator = enumerable
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false)
            .GetAsyncEnumerator();

        do
        {
            try
            {
                if (!await enumerator.MoveNextAsync())
                    break;
            }
            catch (SqlException error)
            {
                throw SqlExceptionHelper.Rethrow(error, cancellationToken);
            }

            Model.EventSubscription subscription = enumerator.Current;
            yield return new Subscription(
                subscription.SubscriptionId,
                subscription.StreamId,
                subscription.Position,
                subscription.ProcessedAt);
        }
        while (true);
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        SubscriptionId subscriptionId,
        StreamId streamId,
        bool failIfExists = true,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.NotNull(streamId);

        var command = new CreateSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            SubscriptionId = subscriptionId,
            StreamId = streamId,
            FailIfExists = failIfExists,
        };

        await command.ExecuteAsync(cancellationToken)
                     .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);

        var command = new DeleteSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            SubscriptionId = subscriptionId,
        };

        int affectedRows = await command.ExecuteAsync(cancellationToken)
                                        .ConfigureAwait(false);

        if (!subscriptionId.IsWildcard && affectedRows == 0)
        {
            throw new SubscriptionNotFoundException(subscriptionId.Value);
        }
    }

    /// <inheritdoc />
    public async Task<WatchSubscriptionResult?> WatchAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        AppCoreNet.Data.ITransaction? transaction = null;

        if (_dataProvider.TransactionManager.CurrentTransaction == null)
        {
            transaction = await _dataProvider.TransactionManager.BeginTransactionAsync(cancellationToken)
                                             .ConfigureAwait(false);
        }

        await using (transaction)
        {
            var procedure = new WatchSubscriptionsStoredProcedure(_dbContext, _options.SchemaName)
            {
                PollInterval = _options.PollInterval,
                Timeout = timeout,
                LockResource = _options.ApplicationName + "-WatchSubscriptions",
            };

            Model.WatchSubscriptionsResult result =
                await procedure.ExecuteAsync(cancellationToken)
                               .ConfigureAwait(false);

            return result.Id == null
                ? null
                : new WatchSubscriptionResult(result.SubscriptionId!, result.StreamId!, (long)result.Position!);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        SubscriptionId subscriptionId,
        long position,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.InRange(position, StreamPosition.Start.Value, long.MaxValue);

        var updateCommand = new UpdateSubscriptionCommand(_dbContext, _options.SchemaName)
        {
            SubscriptionId = subscriptionId,
            Position = position,
        };

        int affectedRows = await updateCommand.ExecuteAsync(cancellationToken)
                                              .ConfigureAwait(false);

        if (affectedRows == 0)
        {
            throw new SubscriptionNotFoundException(subscriptionId.Value);
        }
    }

    /// <inheritdoc />
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        AppCoreNet.Data.ITransaction transaction =
            await _dataProvider
                  .TransactionManager.BeginTransactionAsync(cancellationToken)
                  .ConfigureAwait(false);

        return new StoreTransaction(transaction);
    }
}