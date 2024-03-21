using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

public interface ITransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}