// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppCoreNet.EventStore.Subscription;

/// <summary>
/// Provides options for subscriptions.
/// </summary>
public sealed class SubscriptionOptions
{
    private readonly Dictionary<SubscriptionId, Subscription> _subscriptions = new ();

    internal IEnumerable<(SubscriptionId SubscriptionId, Subscription Subscription)> GetSubscriptions()
    {
        return _subscriptions.Select(s => (s.Key, s.Value));
    }

    /// <summary>
    /// Configures a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listenerFactory">The factory used to create the <see cref="ISubscriptionListener"/>.</param>
    public void Subscribe(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotNull(listenerFactory);

        if (subscriptionId.IsWildcard)
        {
            throw new ArgumentException(
                $"Cannot subscribe to wildcard subscription ID '{subscriptionId}'.",
                nameof(subscriptionId));
        }

        if (_subscriptions.ContainsKey(subscriptionId))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' already exists.");
        }

        _subscriptions.Add(subscriptionId, new Subscription(streamId, listenerFactory));
    }

    /// <summary>
    /// Configures a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listener">The <see cref="ISubscriptionListener"/>.</param>
    public void Subscribe(SubscriptionId subscriptionId, StreamId streamId, ISubscriptionListener listener)
    {
        Ensure.Arg.NotNull(listener);

        Subscribe(
            subscriptionId,
            streamId,
            _ => listener);
    }

    /// <summary>
    /// Configures a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <typeparam name="T">The type of the <see cref="ISubscriptionListener"/>.</typeparam>
    public void Subscribe<T>(SubscriptionId subscriptionId, StreamId streamId)
        where T : ISubscriptionListener
    {
        Subscribe(
            subscriptionId,
            streamId,
            sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp));
    }
}