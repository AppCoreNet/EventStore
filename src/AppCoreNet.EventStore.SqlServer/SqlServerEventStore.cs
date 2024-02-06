using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

public class SqlServerEventStore<TDbContext> : IEventStore
    where TDbContext : DbContext
{
    private DbContextDataProvider<TDbContext> DataProvider { get; }

    public SqlServerEventStore(DbContextDataProvider<TDbContext> dataProvider)
    {
        DataProvider = dataProvider;
    }

    private string SerializeData(IEventEnvelope envelope)
    {
        return JsonSerializer.Serialize(envelope.Data);
    }

    public async Task WriteAsync(
        string streamId,
        IEnumerable<IEventEnvelope> events,
        long? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ITransactionManager transactionManager = DataProvider.TransactionManager;
        ITransaction? transaction = null;

        using var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(Entities.Event.EventType), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(Entities.Event.CreatedAt), typeof(DateTimeOffset)));
        dataTable.Columns.Add(new DataColumn(nameof(Entities.Event.Data), typeof(string)));
        dataTable.Rows.Add(
            events.Select(
                e => new object[]
                {
                    e.Metadata.EventType,
                    e.Metadata.CreatedAt,
                    SerializeData(e),
                }));

        if (transactionManager.CurrentTransaction == null)
        {
            transaction = await transactionManager.BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken)
                                                  .ConfigureAwait(false);
        }

        await using (transaction)
        {
            await DataProvider.DbContext.Database.ExecuteSqlInterpolatedAsync(
                                  $"EXEC sp_EventStore_Append @ExpectedVersion = {expectedVersion}, @Events = {dataTable}",
                                  cancellationToken)
                              .ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }
    }

    public async Task<IReadOnlyCollection<IEventEnvelope>> ReadAsync(
        string streamId,
        long? fromVersion,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task WatchAsync(long? fromOffset, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}