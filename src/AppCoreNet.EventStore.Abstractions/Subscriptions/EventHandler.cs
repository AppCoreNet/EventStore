// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Provides a base class for <see cref="IEventHandler"/> which only invokes the handler method
/// when the event data is of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the event data.</typeparam>
public abstract class EventHandler<T> : IEventHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(EventEnvelope @event, CancellationToken cancellationToken)
    {
        if (@event.Data is T typedEvent)
        {
            await HandleAsync(typedEvent, @event, cancellationToken);
        }
    }

    /// <summary>
    /// Must be overridden to handle the event.
    /// </summary>
    /// <param name="data">The event data.</param>
    /// <param name="event">The <see cref="EventEnvelope"/> which should be handled.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
    protected abstract Task HandleAsync(T data, EventEnvelope @event, CancellationToken cancellationToken);
}