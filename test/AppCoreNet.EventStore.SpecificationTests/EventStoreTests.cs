// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCoreNet.EventStore;

#pragma warning disable IDISP006
#pragma warning disable IDISP003

public abstract class EventStoreTests : IAsyncLifetime
{
    private IServiceProvider? _serviceProvider;
    private AsyncServiceScope _serviceScope;

    protected IEventStore EventStore => _serviceScope.ServiceProvider.GetRequiredService<IEventStore>();

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
        _ => StreamState.Index(value),
    };

    private static StreamPosition CreateStreamPosition(long value) => value switch
    {
        -2 => StreamPosition.End,
        -1 => StreamPosition.Start,
        _ => StreamPosition.FromValue(value),
    };

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    protected virtual Task InitializeAsync()
    {
        _serviceProvider = CreateServiceProvider();
        _serviceScope = _serviceProvider.CreateAsyncScope();
        return Task.CompletedTask;
    }

    protected virtual async Task DisposeAsync()
    {
        await _serviceScope.DisposeAsync();
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(-1L)]
    public async Task WritesInitialEvents(int stateValue)
    {
        string streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name")
        ];

        StreamState state = CreateStreamState(stateValue);
        await EventStore.WriteAsync(streamId, events, state);
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(0L)]
    public async Task WritesAdditionalEvents(int stateValue)
    {
        string streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        events =
        [
            new("TestRenamed", "new_name")
        ];

        StreamState state = CreateStreamState(stateValue);
        await EventStore.WriteAsync(streamId, events, state);
    }

    [Fact]
    public async Task ThrowsWhenWritingInitialEventWithWrongPosition()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await Assert.ThrowsAsync<StreamStateException>(
            async () => await EventStore.WriteAsync(streamId, events, StreamState.Index(0)));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1L)]
    public async Task ThrowsWhenWritingAdditionalEventWithWrongPosition(long stateValue)
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        events =
        [
            new("TestRenamed", "new_name")
        ];

        StreamState state = CreateStreamState(stateValue);

        await Assert.ThrowsAsync<StreamStateException>(
            async () => await EventStore.WriteAsync(streamId, events, state));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartForward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.Start,
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  [events[0], events[1]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    public static IEnumerable<object[]> GetReadsEventsFromWildcardStreamIds()
    {
        string uniqueStreamId = Guid.NewGuid().ToString("N").Substring(0, 4);
        string uniqueStreamId2 = Guid.NewGuid().ToString("N").Substring(0, 4);
        string entityId1 = Guid.NewGuid().ToString("N");
        string entityId2 = Guid.NewGuid().ToString("N");

        return
        [
            [
                $"test-{uniqueStreamId}-*",
                $"test-{uniqueStreamId}-{entityId1}",
                $"{entityId1}-test-{uniqueStreamId}", 2],
            [
                $"*-test-{uniqueStreamId2}",
                $"test-{uniqueStreamId2}-{entityId2}",
                $"{entityId2}-test-{uniqueStreamId2}", 2
            ]
        ];
    }

    [Theory]
    [MemberData(nameof(GetReadsEventsFromWildcardStreamIds))]
    public async Task ReadsEventsFromWildcardStreamStartForward(string readStreamId, string writeStreamId1, string writeStreamId2, int expectedCount)
    {
        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name")
        ];

        await EventStore.WriteAsync(writeStreamId1, events, StreamState.None);
        await EventStore.WriteAsync(writeStreamId2, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            readStreamId,
            StreamPosition.Start,
            StreamReadDirection.Forward,
            int.MaxValue);

        result.Should()
              .HaveCount(expectedCount);

        result.Should()
              .BeEquivalentTo(
                  [events[0], events[1]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndForward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.End,
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(1);

        result.Should()
              .BeEquivalentTo(
                  [events[2]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndBackward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.End,
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  [events[2], events[1]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartBackward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.Start,
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(1);

        result.Should()
              .BeEquivalentTo(
                  [events[0]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    [Fact]
    public async Task ReadsEventsFromStreamForward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.FromValue(1),
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  [events[1], events[2]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
    }

    [Fact]
    public async Task ReadsEventsFromStreamBackward()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name"),
            new("TestRenamed", "new_name_2")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<EventEnvelope> result = await EventStore.ReadAsync(
            streamId,
            StreamPosition.FromValue(1),
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  [events[1], events[0]],
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Index)
                       .Excluding(e => e.Metadata.Sequence));
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
        StreamPosition streamPosition = CreateStreamPosition(positionValue);

        StreamId streamId = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<StreamNotFoundException>(
            async () => await EventStore.ReadAsync(streamId, streamPosition, direction));
    }

    [Fact]
    public async Task WatchWildcardStreamWithoutEventsReturnsNull()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(1);
        Stopwatch watch = new();
        watch.Start();

        WatchEventResult? result = await EventStore.WatchAsync(StreamId.All, StreamPosition.End, timeout);

        result.Should()
              .BeNull();

        watch.Elapsed.Should()
             .BeCloseTo(timeout, TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task WatchWithoutEventsReturnsNull()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        TimeSpan timeout = TimeSpan.FromSeconds(1);
        Stopwatch watch = new();
        watch.Start();

        WatchEventResult? result = await EventStore.WatchAsync(streamId, StreamPosition.End, timeout);

        result.Should()
              .BeNull();

        watch.Elapsed.Should()
             .BeCloseTo(timeout, TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task WatchUnknownStreamThrows()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");
        await Assert.ThrowsAsync<StreamNotFoundException>(
            async () => await EventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task WatchWithEventsReturnsLatestVersion()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name"),
            new("TestRenamed", "new_name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        WatchEventResult? result = await EventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1));

        result.Should()
              .NotBeNull();
    }

    [Fact]
    public async Task WatchWithNewEventsContinues()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        WatchEventResult? result = await EventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1));

        result.Should()
              .NotBeNull();

        events =
        [
            new("TestRenamed", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.Any);

        result = await EventStore.WatchAsync(streamId, result.Position, TimeSpan.FromSeconds(1));

        result.Should()
              .NotBeNull();

        IReadOnlyCollection<EventEnvelope> readEvents = await EventStore.ReadAsync(streamId, result.Position);

        readEvents.Should()
                  .HaveCount(1);

        EventEnvelope readEvent = readEvents.First();

        readEvent.EventTypeName.Should().Be("TestRenamed");
    }

    [Fact]
    public async Task ThrowsWhenDeletingUnknownStream()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<StreamNotFoundException>(
            async () => await EventStore.DeleteAsync(streamId));
    }

    [Fact]
    public async Task ThrowsWhenDeletingAlreadyDeletedStream()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        await EventStore.DeleteAsync(streamId);

        await Assert.ThrowsAsync<StreamDeletedException>(
            async () => await EventStore.DeleteAsync(streamId));
    }

    [Fact]
    public async Task ThrowsWhenReadingEventsFromDeletedStream()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        await EventStore.DeleteAsync(streamId);

        await Assert.ThrowsAsync<StreamDeletedException>(
            async () => await EventStore.ReadAsync(streamId, StreamPosition.Start));
    }

    [Fact]
    public async Task ThrowsWhenWatchingDeletedStream()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        await EventStore.DeleteAsync(streamId);

        await Assert.ThrowsAsync<StreamDeletedException>(
            async () => await EventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task DoesNoThrowWhenReadingEventsByWildcardFromDeletedStream()
    {
        StreamId streamId = "deleted-" + Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        await EventStore.DeleteAsync(streamId);

        await EventStore.ReadAsync(StreamId.Prefix("deleted-"), StreamPosition.Start);
    }

    [Fact]
    public async Task ThrowsWhenWritingEventsToDeletedStream()
    {
        StreamId streamId = Guid.NewGuid().ToString("N");

        EventEnvelope[] events =
        [
            new("TestCreated", "name")
        ];

        await EventStore.WriteAsync(streamId, events, StreamState.None);

        await EventStore.DeleteAsync(streamId);

        await Assert.ThrowsAsync<StreamDeletedException>(
            async () => await EventStore.WriteAsync(streamId, events, StreamState.Any));
    }
}