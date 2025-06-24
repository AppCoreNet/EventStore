// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

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
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// Gets the ID of the stream.
    /// </summary>
    public required string StreamId { get; init; }

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