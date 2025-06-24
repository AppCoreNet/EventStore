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
    private readonly Dictionary<SubscriptionId, Subscriber> _subscribers = new();

    internal Dictionary<SubscriptionId, EventHandlerSubscriptionOptions> EventHandlerOptions { get; } = new();

    /// <summary>
    /// Gets or sets the batch size when processing event subscriptions.
    /// </summary>
    public int BatchSize { get; set; } = 1024;

    /// <summary>
    /// Gets all configured subscriptions.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{T}"/> of <see cref="Subscriber"/>.</returns>
    public IEnumerable<Subscriber> GetSubscribers()
    {
        return _subscribers.Values;
    }

    /// <summary>
    /// Adds a subscription listener.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listenerFactory">The factory used to create the <see cref="ISubscriptionListener"/>.</param>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions AddListener(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory)
    {
        Ensure.Arg.NotNull(subscriptionId);

        if (_subscribers.ContainsKey(subscriptionId))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' already added.");
        }

        _subscribers.Add(subscriptionId, new Subscriber(subscriptionId, streamId, listenerFactory));
        return this;
    }

    /// <summary>
    /// Adds a subscription listener.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="listener">The <see cref="ISubscriptionListener"/>.</param>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions AddListener(
        SubscriptionId subscriptionId,
        StreamId streamId,
        ISubscriptionListener listener)
    {
        Ensure.Arg.NotNull(listener);

        return AddListener(
            subscriptionId,
            streamId,
            _ => listener);
    }

    /// <summary>
    /// Adds a subscription listener.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <typeparam name="T">The type of the <see cref="ISubscriptionListener"/>.</typeparam>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions AddListener<T>(SubscriptionId subscriptionId, StreamId streamId)
        where T : ISubscriptionListener
    {
        return AddListener(
            subscriptionId,
            streamId,
            sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp));
    }

    /// <summary>
    /// Adds a subscription listener which dispatches events to <see cref="IEventHandler"/>.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="streamId">The stream ID.</param>
    /// <param name="configure">Delegate which is invoked to configure the event handlers.</param>
    /// <returns>The <see cref="SubscriptionOptions"/> instance to allow chaining.</returns>
    public SubscriptionOptions AddEventHandlers(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Action<EventHandlerSubscriptionOptions> configure)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotNull(configure);

        if (_subscribers.TryGetValue(subscriptionId, out Subscriber? subscriber))
        {
            if (subscriber.StreamId != streamId)
                throw new InvalidOperationException();

            if (!EventHandlerOptions.ContainsKey(subscriptionId))
                throw new InvalidOperationException();
        }
        else
        {
            AddListener<EventHandlerSubscriptionListener>(subscriptionId, streamId);
        }

        if (!EventHandlerOptions.TryGetValue(subscriptionId, out EventHandlerSubscriptionOptions? options))
        {
            options = new EventHandlerSubscriptionOptions();
            EventHandlerOptions.Add(subscriptionId, options);
        }

        configure(options);
        return this;
    }
}