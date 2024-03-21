// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal abstract class SqlCommand<T>
{
    protected DbContext DbContext { get; }

    protected SqlCommand(DbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<T> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteCoreAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SqlException error)
        {
            if (error.Number == 3980 && cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(null, error);

            throw new EventStoreException($"An error occured accessing the event store: {error.Message}", error);
        }
    }

    protected abstract Task<T> ExecuteCoreAsync(CancellationToken cancellationToken);
}