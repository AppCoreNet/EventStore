using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.EventStore.Serialization;
using AppCoreNet.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[Collection(SqlServerTestCollection.Name)]
[Trait("Category", "Integration")]
public class SqlServerEventStoreTests
{
    private readonly SqlServerTestFixture _sqlServerTestFixture;

    public SqlServerEventStoreTests(SqlServerTestFixture sqlServerTestFixture)
    {
        _sqlServerTestFixture = sqlServerTestFixture;
    }

    protected ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    protected void ConfigureServices(IServiceCollection services)
    {
        services.TryAddSingleton(Substitute.For<IEntityMapper>());

        services.Configure<JsonEventStoreSerializerOptions>(
            o =>
            {
                o.EventTypeMap.Add("TestCreated", typeof(string));
                o.EventTypeMap.Add("TestRenamed", typeof(string));
            });

        services.TryAddSingleton<IEventStoreSerializer, JsonEventStoreSerializer>();

        services.AddDataProvider(
            p =>
            {
                p.AddDbContext<TestDbContext>(
                    o => { o.UseSqlServer(_sqlServerTestFixture.ConnectionString); });
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

    private async Task<SqlServerEventStore<TestDbContext>> CreateEventStore(IServiceProvider sp)
    {
        var provider = sp.GetRequiredService<DbContextDataProvider<TestDbContext>>();
        await provider.DbContext.Database.MigrateAsync();
        return new SqlServerEventStore<TestDbContext>(provider, sp.GetRequiredService<IEventStoreSerializer>());
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(-1L)]
    public async Task WritesInitialEvents(int stateValue)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

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

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

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

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
        };

        await Assert.ThrowsAsync<EventStreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, StreamState.Position(0)));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1L)]
    public async Task ThrowsWhenWritingAdditionalEventWithWrongPosition(long stateValue)
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

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

        await Assert.ThrowsAsync<EventStreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, state));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamForward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
    }

    [Fact]
    public async Task ReadsEventsFromStreamBackward()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
            new EventEnvelope("TestRenamed", "new_name_2"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
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
                       .Excluding(e => e.Metadata.StreamPosition));
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

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        StreamPosition streamPosition = CreateStreamPosition(positionValue);

        string streamId = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<EventStreamNotFoundException>(
            async () => await eventStore.ReadAsync(streamId, streamPosition, direction));
    }

    [Fact]
    public async Task WatchWithoutEventsReturnsNull()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);
        await eventStore.DeleteAsync("$all");

        TimeSpan timeout = TimeSpan.FromSeconds(1);
        Stopwatch watch = new ();
        watch.Start();

        WatchResult? result = await eventStore.WatchAsync("$all", StreamPosition.Start, timeout);

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

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);
        await eventStore.DeleteAsync("$all");

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("TestCreated", "name"),
            new EventEnvelope("TestRenamed", "new_name"),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        WatchResult? result = await eventStore.WatchAsync(streamId, StreamPosition.Start, TimeSpan.FromSeconds(1));

        result.Should()
              .NotBeNull();

        result!.Position.Should()
              .Be(1);

        WatchResult lastResult = result;

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