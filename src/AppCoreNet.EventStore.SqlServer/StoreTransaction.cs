// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.SqlServer;

[SuppressMessage(
    "IDisposableAnalyzers.Correctness",
    "IDISP007:Don\'t dispose injected",
    Justification = "Transaction is owned by the StoreTransaction instance")]
internal sealed class StoreTransaction : ITransaction, IDisposable, IAsyncDisposable
{
    private readonly Data.ITransaction _transaction;

    public StoreTransaction(Data.ITransaction transaction)
    {
        _transaction = transaction;
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync()
                          .ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken)
                          .ConfigureAwait(false);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken)
                          .ConfigureAwait(false);
    }
}