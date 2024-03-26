// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Provides information about a subscription.
/// </summary>
public sealed class Subscription
{
    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    public SubscriptionId Id { get; }

    /// <summary>
    /// Gets the subscribed stream.
    /// </summary>
    public StreamId StreamId { get; }

    /// <summary>
    /// Gets the current position of the subscription.
    /// </summary>
    public StreamPosition Position { get; }

    /// <summary>
    /// Gets the date and time of the last processing.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscription"/> class.
    /// </summary>
    /// <param name="id">The ID of the subscription.</param>
    /// <param name="streamId">The subscribed stream.</param>
    /// <param name="position">The current position of the subscription.</param>
    /// <param name="processedAt">Date and time of the last processing.</param>
    public Subscription(SubscriptionId id, StreamId streamId, StreamPosition position, DateTimeOffset? processedAt)
    {
        Ensure.Arg.NotNull(id);
        Ensure.Arg.NotWildcard(id);
        Ensure.Arg.NotNull(streamId);

        Id = id;
        StreamId = streamId;
        Position = position;
        ProcessedAt = processedAt;
    }
}