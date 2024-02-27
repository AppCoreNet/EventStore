using System;
using System.Threading.Tasks;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCoreNet.EventStore;

public abstract class SubscriptionManagerTests
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

    [Fact]
    public async Task CreatesSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var manager = sp.GetRequiredService<ISubscriptionManager>();

        await manager.CreateAsync(Guid.NewGuid().ToString("N"), "$all");
    }

    [Fact]
    public async Task WatchReturnsSubscription()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        string subscriptionId = Guid.NewGuid().ToString("N");
        StreamId streamId = Guid.NewGuid().ToString("N");

        var eventStore = sp.GetRequiredService<IEventStore>();
        await eventStore.DeleteAsync(StreamId.All);
        await eventStore.WriteAsync(
            streamId, new[] { new EventEnvelope("TestCreated", "name") }, StreamState.None);

        var manager = sp.GetRequiredService<ISubscriptionManager>();

        await manager.DeleteAsync("$all");
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

    protected abstract Task ProcessSubscriptionsAsync(ISubscriptionManager manager);

    [Fact]
    public async Task ProcessesSubscriptions()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        string streamId = Guid.NewGuid().ToString("N");

        var eventStore = sp.GetRequiredService<IEventStore>();
        await eventStore.WriteAsync(
            streamId, new[] { new EventEnvelope("TestCreated", "name") }, StreamState.None);

        var manager = sp.GetRequiredService<ISubscriptionManager>();

        await manager.CreateAsync(Guid.NewGuid().ToString("N"), streamId);

        await ProcessSubscriptionsAsync(manager);
    }
}