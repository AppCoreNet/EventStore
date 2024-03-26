// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Exception which is thrown when a subscription was not found.
/// </summary>
public class SubscriptionNotFoundException : EventStoreException
{
    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionNotFoundException"/> class.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    public SubscriptionNotFoundException(string subscriptionId)
        : base($"Event subscription '{subscriptionId}' not found.")
    {
        Ensure.Arg.NotEmpty(subscriptionId);
        SubscriptionId = subscriptionId;
    }
}