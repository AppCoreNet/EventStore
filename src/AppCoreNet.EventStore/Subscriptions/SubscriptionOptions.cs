// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Provides options for subscriptions.
/// </summary>
public sealed class SubscriptionOptions
{
    private readonly Dictionary<SubscriptionId, Subscriber> _subscribers = new ();

    /// <summary>
    /// Gets all configured subscriptions.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{T}"/> of <see cref="Subscriber"/>.</returns>
    public IEnumerable<Subscriber> GetSubscribers()
    {
        return _subscribers.Values;
    }

    /// <summary>
    /// Adds a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listenerFactory">The factory used to create the <see cref="ISubscriptionListener"/>.</param>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions Add(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotNull(listenerFactory);

        if (_subscribers.ContainsKey(subscriptionId))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' already added.");
        }

        _subscribers.Add(subscriptionId, new Subscriber(subscriptionId, streamId, listenerFactory));
        return this;
    }

    /// <summary>
    /// Adds a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listener">The <see cref="ISubscriptionListener"/>.</param>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions Add(
        SubscriptionId subscriptionId,
        StreamId streamId,
        ISubscriptionListener listener)
    {
        Ensure.Arg.NotNull(listener);

        return Add(
            subscriptionId,
            streamId,
            _ => listener);
    }

    /// <summary>
    /// Adds a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <typeparam name="T">The type of the <see cref="ISubscriptionListener"/>.</typeparam>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions Add<T>(SubscriptionId subscriptionId, StreamId streamId)
        where T : ISubscriptionListener
    {
        return Add(
            subscriptionId,
            streamId,
            sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp));
    }
}