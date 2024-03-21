// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class WatchSubscriptionsResult
{
    public int? Id { get; set; }

    public string? SubscriptionId { get; set; }

    public long? Position { get; set; }

    public string? StreamId { get; set; }
}