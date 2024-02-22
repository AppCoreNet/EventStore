using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

public interface ISubscriptionManager
{
    Task CreateAsync(
        string id,
        StreamId streamId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<WatchSubscriptionsResult?> WatchAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}