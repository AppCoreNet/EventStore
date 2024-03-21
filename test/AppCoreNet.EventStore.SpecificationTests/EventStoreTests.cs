// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCoreNet.EventStore;

public abstract class EventStoreTests : IAsyncLifetime
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

    private static StreamState CreateStreamState(long value) => value switch
    {
        -2 => StreamState.Any,
        -1 => StreamState.None,
        _ => StreamState.Position(value)
    };

    private static StreamPosition CreateStreamPosition(long value) => value switch
    {
        -2 => StreamPosition.End,
        -1 => StreamPosition.Start,
        _ => StreamPosition.FromValue(value)
    };

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

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        await eventStore.DeleteAsync(StreamId.All);
    }

    protected virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(-1L)]
    public async Task WritesInitialEvents(int stateValue)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
        };

        StreamState state = CreateStreamState(stateValue);
        await eventStore.WriteAsync(streamId, events, state);
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(0L)]
    public async Task WritesAdditionalEvents(int stateValue)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        events = new[]
        {
            new EventEnvelope("TestRenamed", "new_name"),
        };

        StreamState state = CreateStreamState(stateValue);
        await eventStore.WriteAsync(streamId, events, state);
    }

    [Fact]
    public async Task ThrowsWhenWritingInitialEventWithWrongPosition()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
        };

        await Assert.ThrowsAsync<StreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, StreamState.Position(0)));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1L)]
    public async Task ThrowsWhenWritingAdditionalEventWithWrongPosition(long stateValue)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        events = new[]
        {
            new EventEnvelope("TestRenamed", "new_name"),
        };

        StreamState state = CreateStreamState(stateValue);

        await Assert.ThrowsAsync<StreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, state));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.Start,
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[0], events[1] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.End,
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(1);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[2] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.End,
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[2], events[1] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.Start,
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(1);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[0] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.FromValue(1),
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[1], events[2] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.FromValue(1),
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[1], events[0] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.StreamPosition)
                       .Excluding(e => e.Metadata.GlobalPosition));
    }

    [Theory]
    [InlineData(-2, StreamReadDirection.Forward)]
    [InlineData(-2, StreamReadDirection.Backward)]
    [InlineData(-1, StreamReadDirection.Forward)]
    [InlineData(-1, StreamReadDirection.Backward)]
    [InlineData(0, StreamReadDirection.Forward)]
    [InlineData(0, StreamReadDirection.Backward)]
    public async Task ReadsEventsFromUnknownStreamThrows(long positionValue, StreamReadDirection direction)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamPosition streamPosition = CreateStreamPosition(positionValue);

        StreamId streamId = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<StreamNotFoundException>(
            async () => await eventStore.ReadAsync(streamId, streamPosition, direction));
    }

    [Fact]
    public async Task WatchWithoutEventsReturnsNull()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        TimeSpan timeout = TimeSpan.FromSeconds(1);
        Stopwatch watch = new ();
        watch.Start();

        WatchEventResult? result = await eventStore.WatchAsync("$all", StreamPosition.Start, timeout);

        result.Should()
              .BeNull();

        watch.Elapsed.Should()
             .BeCloseTo(timeout, TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task WatchWithEventsReturnsLatestVersion()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        StreamId streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        WatchEventResult? result = await eventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1));

        result.Should()
              .NotBeNull();

        result!.Position.Should()
              .Be(1);

        WatchEventResult lastResult = result;

        result = await eventStore.WatchAsync(streamId, result.Position, TimeSpan.FromSeconds(1));

        result.Should()
              .BeNull();

        await eventStore.WriteAsync(streamId + "-2", events, StreamState.None);
        await eventStore.WriteAsync(streamId, events, StreamState.Position(1));

        result = await eventStore.WatchAsync(streamId, lastResult.Position, TimeSpan.FromSeconds(5));
        lastResult = result!;

        _ = await eventStore.WatchAsync(streamId, result!.Position, TimeSpan.FromSeconds(5));
    }
}