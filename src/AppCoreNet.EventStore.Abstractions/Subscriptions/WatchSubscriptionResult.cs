// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Represents the result of <see cref="ISubscriptionStore.WatchAsync"/>.
/// </summary>
public sealed class WatchSubscriptionResult
{
    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    public SubscriptionId SubscriptionId { get; }

    /// <summary>
    /// Gets the ID of the subscribed stream.
    /// </summary>
    public StreamId StreamId { get; }

    /// <summary>
    /// Gets the position of the last event that was processed by the subscription.
    /// </summary>
    /// <remarks>
    /// The position refers to the <see cref="EventMetadata.Index"/> when the subscription was created
    /// for a specific stream. If the subscription was created for a wildcard stream it refers to the
    /// <see cref="EventMetadata.Sequence"/>.
    /// </remarks>
    public StreamPosition Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchSubscriptionResult"/> class.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="streamId">The ID of the subscribed stream.</param>
    /// <param name="position">The position of the last event that was processed for the subscription.</param>
    public WatchSubscriptionResult(SubscriptionId subscriptionId, StreamId streamId, StreamPosition position)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.NotNull(streamId);

        SubscriptionId = subscriptionId;
        StreamId = streamId;
        Position = position;
    }
}