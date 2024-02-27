using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents the result of <see cref="ISubscriptionManager.WatchAsync"/>.
/// </summary>
public sealed class WatchSubscriptionResult
{
    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the ID of the subscribed stream.
    /// </summary>
    public StreamId StreamId { get; }

    /// <summary>
    /// Gets the position of the next event to be processed by the subscription.
    /// </summary>
    /// <remarks>
    /// The position refers to the <see cref="EventMetadata.StreamPosition"/> when watch was invoked
    /// for a specific stream. If watch was invoked for a wildcard stream it refers to the
    /// <see cref="EventMetadata.GlobalPosition"/>.
    /// </remarks>
    public long Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchSubscriptionResult"/> class.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="streamId">The ID of the subscribed stream.</param>
    /// <param name="position">The position of the next event to be processed by the subscription.</param>
    public WatchSubscriptionResult(string subscriptionId, StreamId streamId, long position)
    {
        Ensure.Arg.NotEmpty(subscriptionId);
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.InRange(position, 0, long.MaxValue);

        SubscriptionId = subscriptionId;
        StreamId = streamId;
        Position = position;
    }
}