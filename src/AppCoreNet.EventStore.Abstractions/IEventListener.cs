using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore;

public interface IEventListener
{
    Task InvokeAsync(IEventEnvelope @event, CancellationToken cancellationToken = default);
}