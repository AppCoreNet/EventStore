using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCoreNet.EventStore;

public abstract class SubscriptionManagerTests : IAsyncLifetime
{
    protected ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
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

        var manager = scope.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        await manager.DeleteAsync(SubscriptionId.All);

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
        };
        await eventStore.WriteAsync(streamId, events, StreamState.None);
    }

    [Fact]
    public async Task CreatesSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        await manager.CreateAsync(Guid.NewGuid().ToString("N"), StreamId.All);
    }

    [Fact]
    public async Task WatchReturnsSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SubscriptionId subscriptionId = Guid.NewGuid().ToString("N");
        StreamId streamId = Guid.NewGuid().ToString("N");

        await CreateEvents(scope.ServiceProvider, streamId);

        var manager = scope.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        await manager.CreateAsync(subscriptionId, streamId);

        WatchSubscriptionResult? result = await manager.WatchAsync(TimeSpan.FromSeconds(5));

        result.Should()
              .NotBeNull();

        result!.SubscriptionId.Should()
               .Be(subscriptionId);

        result.StreamId.Should()
              .Be(streamId);

        result.Position.Should()
              .Be(0);
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

        var manager1 = scope1.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        await manager1.CreateAsync(subscriptionId, StreamId.All);

        using IDisposable transaction1 = await BeginTransaction(scope1.ServiceProvider);
        WatchSubscriptionResult? watch1 = await manager1.WatchAsync(TimeSpan.FromSeconds(0));

        watch1.Should()
              .NotBeNull();

        var manager2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        using IDisposable transaction2 = await BeginTransaction(scope2.ServiceProvider);
        WatchSubscriptionResult? watch2 = await manager2.WatchAsync(TimeSpan.FromSeconds(0));

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

        var manager = scope.ServiceProvider.GetRequiredService<ISubscriptionManager>();
        await manager.CreateAsync(subscriptionId, streamId);

        using IDisposable transaction = await BeginTransaction(scope.ServiceProvider);
        WatchSubscriptionResult? watch = await manager.WatchAsync(TimeSpan.FromSeconds(0));

        watch.Should()
             .NotBeNull();

        IReadOnlyCollection<IEventEnvelope> events = await eventStore.ReadAsync(watch!.StreamId, watch.Position);
        await manager.UpdateAsync(subscriptionId, events.Last().Metadata.GlobalPosition);

        await CommitTransaction(transaction);

        using IDisposable transaction2 = await BeginTransaction(scope.ServiceProvider);
        watch = await manager.WatchAsync(TimeSpan.FromSeconds(0));

        watch.Should()
             .BeNull();
    }

    protected abstract Task<IDisposable> BeginTransaction(IServiceProvider serviceProvider);

    protected abstract Task CommitTransaction(IDisposable transaction);
}