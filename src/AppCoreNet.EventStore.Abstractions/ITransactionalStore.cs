using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

public interface ITransactionalStore
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}