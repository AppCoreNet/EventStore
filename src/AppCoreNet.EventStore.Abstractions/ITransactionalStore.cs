// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents a transactional store.
/// </summary>
public interface ITransactionalStore
{
    /// <summary>
    /// Begins a transaction.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The transaction.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}