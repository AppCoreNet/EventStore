// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

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
            throw SqlExceptionHelper.Rethrow(error, cancellationToken);
        }
    }

    protected abstract Task<T> ExecuteCoreAsync(CancellationToken cancellationToken);
}