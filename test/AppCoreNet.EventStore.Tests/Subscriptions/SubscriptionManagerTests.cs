// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.Subscriptions;

public sealed class SubscriptionManagerTests : IDisposable
{
    private ServiceProvider ServiceProvider { get; }

    private ISubscriptionStore Store { get; }

    private ISubscriptionListener Listener { get; }

    public SubscriptionManagerTests()
    {
        Store = Substitute.For<ISubscriptionStore>();
        Listener = Substitute.For<ISubscriptionListener>();
        ServiceProvider = CreateServiceProvider(
            services =>
            {
                services.AddSingleton(Store);
            });
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }

    private static ServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);
        return services.BuildServiceProvider();
    }

    private SubscriptionManager CreateSubscriptionManager(SubscriptionOptions? options = null)
    {
        return new SubscriptionManager(
            ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options ?? new SubscriptionOptions()));
    }

    [Fact]
    public async Task SubscribeCreatesSubscription()
    {
        SubscriptionManager manager = CreateSubscriptionManager();

        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        await manager.SubscribeAsync(subscriptionId, streamId, _ => Listener);

        await Store.Received(1)
                   .CreateAsync(subscriptionId, streamId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnsubscribeDeletesSubscription()
    {
        SubscriptionManager manager = CreateSubscriptionManager();

        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        await manager.SubscribeAsync(subscriptionId, streamId, _ => Listener);
        await manager.UnsubscribeAsync(subscriptionId);

        await Store.Received(1)
                   .DeleteAsync(subscriptionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializesSubscriptionsFromOptions()
    {
        var subscriptionId1 = SubscriptionId.NewId();
        var subscriptionId2 = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        var options = new SubscriptionOptions();
        options.Subscribe(subscriptionId1, streamId, _ => Listener);
        options.Subscribe(subscriptionId2, streamId, _ => Listener);

        SubscriptionManager manager = CreateSubscriptionManager(options);
        await manager.InitializeAsync();

        await Store.Received(1)
                   .CreateAsync(subscriptionId1, streamId, false, Arg.Any<CancellationToken>());

        await Store.Received(1)
                   .CreateAsync(subscriptionId2, streamId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatesRegisteredListener()
    {
        SubscriptionManager manager = CreateSubscriptionManager();

        var subscriptionId = SubscriptionId.NewId();
        StreamId streamId = StreamId.All;

        await manager.SubscribeAsync(subscriptionId, streamId, _ => Listener);
        ISubscriptionListener listener = manager.CreateListener(subscriptionId, ServiceProvider);

        listener.Should()
                .BeSameAs(Listener);
    }

    [Fact]
    public void CreatesListenerForUnknownSubscription()
    {
        SubscriptionManager manager = CreateSubscriptionManager();

        var subscriptionId = SubscriptionId.NewId();
        ISubscriptionListener listener = manager.CreateListener(subscriptionId, ServiceProvider);

        listener.Should()
                .NotBeNull();
    }
}