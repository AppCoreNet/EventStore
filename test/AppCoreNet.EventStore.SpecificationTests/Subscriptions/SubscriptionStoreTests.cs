// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.Subscriptions;

public abstract class SubscriptionStoreTests : IAsyncLifetime
{
    protected ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ApplicationName.Returns(typeof(SubscriptionStoreTests).Assembly.FullName);

        services.AddTransient<IHostEnvironment>(_ => hostEnvironment);

        services.AddEventStore()
                .AddJsonSerializer(
                    o =>
                    {
                        o.TypeNameMap.Add("TestCreated", typeof(string));
                        o.TypeNameMap.Add("TestRenamed", typeof(string));
                    });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    protected virtual async Task InitializeAsync()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        await subscriptionStore.DeleteAsync(SubscriptionId.All);

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        await eventStore.DeleteAsync(StreamId.All);
    }

    protected virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task CreateEvents(IServiceProvider serviceProvider, StreamId streamId)
    {
        var eventStore = serviceProvider.GetRequiredService<IEventStore>();
        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "name"),
        };
        await eventStore.WriteAsync(streamId, events, StreamState.None);
    }

    [Fact]
    public async Task CreatesSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        await subscriptionStore.CreateAsync(Guid.NewGuid().ToString("N"), StreamId.All);
    }

    [Fact]
    public async Task CreateExistingSubscriptionDoesNotThrow()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();

        string subscriptionId = Guid.NewGuid().ToString("N");
        await subscriptionStore.CreateAsync(subscriptionId, StreamId.All);
        await subscriptionStore.CreateAsync(subscriptionId, StreamId.All, failIfExists: false);
    }

    [Fact]
    public async Task CreateExistingSubscriptionThrows()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();

        string subscriptionId = Guid.NewGuid().ToString("N");
        await subscriptionStore.CreateAsync(subscriptionId, StreamId.All);

        await Assert.ThrowsAsync<EventStoreException>(
            async () =>
                await subscriptionStore.CreateAsync(subscriptionId, StreamId.All, failIfExists: true));
    }

    [Fact]
    public async Task WatchReturnsSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SubscriptionId subscriptionId = Guid.NewGuid().ToString("N");
        StreamId streamId = Guid.NewGuid().ToString("N");

        await CreateEvents(scope.ServiceProvider, streamId);

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        await subscriptionStore.CreateAsync(subscriptionId, streamId);

        WatchSubscriptionResult? result = await subscriptionStore.WatchAsync(TimeSpan.FromSeconds(5));

        result.Should()
              .NotBeNull();

        result!.SubscriptionId.Should()
               .Be(subscriptionId);

        result.StreamId.Should()
              .Be(streamId);

        result.Position.Should()
              .Be(StreamPosition.Start);
    }

    [Fact]
    public async Task ConcurrentWatchOnlySucceedsOnce()
    {
        await using ServiceProvider sp1 = CreateServiceProvider();
        await using AsyncServiceScope scope1 = sp1.CreateAsyncScope();
        await using AsyncServiceScope scope2 = sp1.CreateAsyncScope();

        SubscriptionId subscriptionId = Guid.NewGuid().ToString("N");
        StreamId streamId = Guid.NewGuid().ToString("N");

        await CreateEvents(scope1.ServiceProvider, streamId);

        var subscriptionStore1 = scope1.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        await subscriptionStore1.CreateAsync(subscriptionId, StreamId.All);

        using IDisposable? transaction1 = subscriptionStore1 is ITransactionalStore transactionalStore1
            ? await transactionalStore1.BeginTransactionAsync()
            : null;

        WatchSubscriptionResult? watch1 = await subscriptionStore1.WatchAsync(TimeSpan.FromSeconds(0));

        watch1.Should()
              .NotBeNull();

        var subscriptionStore2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        using IDisposable? transaction2 = subscriptionStore2 is ITransactionalStore transactionalStore2
            ? await transactionalStore2.BeginTransactionAsync()
            : null;

        WatchSubscriptionResult? watch2 = await subscriptionStore2.WatchAsync(TimeSpan.FromSeconds(0));

        watch2.Should()
              .BeNull();
    }

    [Fact]
    public async Task WatchAndUpdateSucceedsOnceWithoutNewEvents()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SubscriptionId subscriptionId = Guid.NewGuid().ToString("N");
        StreamId streamId = Guid.NewGuid().ToString("N");

        await CreateEvents(scope.ServiceProvider, streamId);

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        await subscriptionStore.CreateAsync(subscriptionId, streamId);

        await using ITransaction? transaction = subscriptionStore is ITransactionalStore transactionalStore1
            ? await transactionalStore1.BeginTransactionAsync()
            : null;

        WatchSubscriptionResult? watch = await subscriptionStore.WatchAsync(TimeSpan.FromSeconds(0));

        watch.Should()
             .NotBeNull();

        IReadOnlyCollection<EventEnvelope> events = await eventStore.ReadAsync(watch!.StreamId, watch.Position);
        await subscriptionStore.UpdateAsync(subscriptionId, events.Last().Metadata.Sequence);

        if (transaction != null)
            await transaction.CommitAsync();

        await using ITransaction? transaction2 = subscriptionStore is ITransactionalStore transactionalStore2
            ? await transactionalStore2.BeginTransactionAsync()
            : null;

        watch = await subscriptionStore.WatchAsync(TimeSpan.FromSeconds(0));

        watch.Should()
             .BeNull();
    }
}