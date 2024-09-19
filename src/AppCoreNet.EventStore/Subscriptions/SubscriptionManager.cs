// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Provides the default implementation of <see cref="ISubscriptionManager"/>.
/// </summary>
public sealed class SubscriptionManager : ISubscriptionManager
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SubscriptionOptions _options;
    private readonly ConcurrentDictionary<SubscriptionId, Subscriber> _subscriptions = new ();

    private sealed class NoOpListener : ISubscriptionListener
    {
        public static readonly NoOpListener Instance = new ();

        public Task HandleAsync(
            SubscriptionId subscriptionId,
            EventEnvelope @event,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionManager"/> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/>.</param>
    /// <param name="options">The <see cref="SubscriptionOptions"/>.</param>
    public SubscriptionManager(IServiceScopeFactory serviceScopeFactory, IOptions<SubscriptionOptions> options)
    {
        Ensure.Arg.NotNull(serviceScopeFactory);
        Ensure.Arg.NotNull(options);

        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
    }

    /// <summary>
    /// Initializes all subscriptions registered via <see cref="SubscriptionOptions"/>.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
        IServiceProvider serviceProvider = serviceScope.ServiceProvider;

        await using (serviceScope.ConfigureAwait(false))
        {
            var store = serviceProvider.GetRequiredService<ISubscriptionStore>();

            foreach (Subscriber subscriber in _options.GetSubscribers())
            {
                _subscriptions.TryAdd(subscriber.SubscriptionId, subscriber);

                await store.CreateAsync(
                               subscriber.SubscriptionId,
                               subscriber.StreamId,
                               failIfExists: false,
                               cancellationToken)
                           .ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        SubscriptionId subscriptionId,
        StreamId streamId,
        Func<IServiceProvider, ISubscriptionListener> listenerFactory,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotNull(listenerFactory);

        if (!_subscriptions.TryAdd(subscriptionId, new Subscriber(subscriptionId, streamId, listenerFactory)))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' already exists.");
        }

        try
        {
            AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
            IServiceProvider serviceProvider = serviceScope.ServiceProvider;

            await using (serviceScope.ConfigureAwait(false))
            {
                var store = serviceProvider.GetRequiredService<ISubscriptionStore>();
                await store.CreateAsync(subscriptionId, streamId, true, cancellationToken)
                           .ConfigureAwait(false);
            }
        }
        catch
        {
            _subscriptions.TryRemove(subscriptionId, out _);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);

        if (!_subscriptions.TryGetValue(subscriptionId, out _))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' does not exist.");
        }

        AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
        IServiceProvider serviceProvider = serviceScope.ServiceProvider;

        await using (serviceScope.ConfigureAwait(false))
        {
            var store = serviceProvider.GetRequiredService<ISubscriptionStore>();
            await store.DeleteAsync(subscriptionId, cancellationToken)
                       .ConfigureAwait(false);
        }

        _subscriptions.TryRemove(subscriptionId, out _);
    }

    /// <inheritdoc />
    public async Task ResubscribeAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotWildcard(subscriptionId);

        if (!_subscriptions.TryGetValue(subscriptionId, out _))
        {
            throw new InvalidOperationException($"Subscription with ID '{subscriptionId}' does not exist.");
        }

        AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
        IServiceProvider serviceProvider = serviceScope.ServiceProvider;

        await using (serviceScope.ConfigureAwait(false))
        {
            var store = serviceProvider.GetRequiredService<ISubscriptionStore>();
            await store.UpdateAsync(subscriptionId, StreamPosition.Start.Value, cancellationToken)
                       .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates the listener for the specified subscription.
    /// </summary>
    /// <remarks>
    /// If no subscription for the specified <paramref name="subscriptionId"/> exists, a listener
    /// which does nothing is being returned.
    /// </remarks>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create the listener.</param>
    /// <returns>The <see cref="ISubscriptionListener"/>.</returns>
    public ISubscriptionListener CreateListener(SubscriptionId subscriptionId, IServiceProvider serviceProvider)
    {
        Ensure.Arg.NotNull(subscriptionId);
        Ensure.Arg.NotNull(serviceProvider);

        return !_subscriptions.TryGetValue(subscriptionId, out Subscriber? subscription)
            ? NoOpListener.Instance
            : subscription.ListenerFactory(serviceProvider);
    }
}