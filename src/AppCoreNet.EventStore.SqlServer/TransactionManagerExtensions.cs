using System;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Data;

namespace AppCoreNet.EventStore.SqlServer;

internal static class TransactionManagerExtensions
{
    public static async Task<T> ExecuteAsync<T>(
        this ITransactionManager transactionManager,
        Func<CancellationToken, Task<T>> queryAction,
        CancellationToken cancellationToken)
    {
        AppCoreNet.Data.ITransaction? transaction = null;

        if (transactionManager.CurrentTransaction == null)
        {
            transaction = await transactionManager
                                .BeginTransactionAsync(cancellationToken)
                                .ConfigureAwait(false);
        }

        T result;
        await using (transaction)
        {
            result = await queryAction(cancellationToken)
                .ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }

        return result;
    }
}