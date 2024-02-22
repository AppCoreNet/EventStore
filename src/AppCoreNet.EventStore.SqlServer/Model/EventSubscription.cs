using System;

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class EventSubscription
{
    /// <summary>
    /// Gets the internal ID of the subscription.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    required public string SubscriptionId { get; init; }

    /// <summary>
    /// Gets the ID of the stream.
    /// </summary>
    required public string StreamId { get; init; }

    /// <summary>
    /// Gets the date and time when the subscription was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the date and time when the subscription was processed.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; init; }

    /// <summary>
    /// Gets the stream position of the subscription.
    /// </summary>
    public long Position { get; init; }
}