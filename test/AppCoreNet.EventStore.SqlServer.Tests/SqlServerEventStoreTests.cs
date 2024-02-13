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
        _ => StreamPosition.Position(value)
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
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
        };

        StreamState state = CreateStreamState(stateValue);
        await eventStore.WriteAsync(streamId, events, state);
    }

    [Theory]
    [InlineData(-2L)]
    [InlineData(0L)]
    public async Task WritesAdditionalEvents(int stateValue)
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        events = new[]
        {
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
        };

        StreamState state = CreateStreamState(stateValue);
        await eventStore.WriteAsync(streamId, events, state);
    }

    [Fact]
    public async Task ThrowsWhenWritingInitialEventWithWrongPosition()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
        };

        await Assert.ThrowsAsync<EventStreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, StreamState.Position(0)));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1L)]
    public async Task ThrowsWhenWritingAdditionalEventWithWrongPosition(long stateValue)
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        events = new[]
        {
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
        };

        StreamState state = CreateStreamState(stateValue);

        await Assert.ThrowsAsync<EventStreamStateException>(
            async () => await eventStore.WriteAsync(streamId, events, state));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartForward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
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
                       .Excluding(e => e.Metadata.Position));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndForward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
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
                       .Excluding(e => e.Metadata.Position));
    }

    [Fact]
    public async Task ReadsEventsFromStreamEndBackward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
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
                       .Excluding(e => e.Metadata.Position));
    }

    [Fact]
    public async Task ReadsEventsFromStreamStartBackward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
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
                       .Excluding(e => e.Metadata.Position));
    }

    [Fact]
    public async Task ReadsEventsFromStreamForward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.Position(1),
            StreamReadDirection.Forward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[1], events[2] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Position));
    }

    [Fact]
    public async Task ReadsEventsFromStreamBackward()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
            new EventEnvelope("new_name_2", new EventMetadata("TestRenamed")),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        IReadOnlyCollection<IEventEnvelope> result = await eventStore.ReadAsync(
            streamId,
            StreamPosition.Position(1),
            StreamReadDirection.Backward,
            2);

        result.Should()
              .HaveCount(2);

        result.Should()
              .BeEquivalentTo(
                  new[] { events[1], events[0] },
                  o =>
                      o.Excluding(e => e.Metadata.CreatedAt)
                       .Excluding(e => e.Metadata.Position));
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
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);

        StreamPosition streamPosition = CreateStreamPosition(positionValue);

        string streamId = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<EventStreamNotFoundException>(
            async () => await eventStore.ReadAsync(streamId, streamPosition, direction));
    }

    [Fact]
    public async Task WatchWithoutEventsReturnsNull()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);
        await eventStore.DeleteAsync("$all");

        TimeSpan timeout = TimeSpan.FromSeconds(5);
        Stopwatch watch = new ();
        watch.Start();

        WatchResult? result = await eventStore.WatchAsync(null, timeout);

        result.Should()
              .BeNull();

        watch.Elapsed.Should()
             .BeCloseTo(timeout, TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task WatchWithEventsReturnsLatestVersion()
    {
        using ServiceProvider sp = CreateServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        SqlServerEventStore<TestDbContext> eventStore = await CreateEventStore(scope.ServiceProvider);
        await eventStore.DeleteAsync("$all");

        string streamId = Guid.NewGuid().ToString("N");

        var events = new[]
        {
            new EventEnvelope("name", new EventMetadata("TestCreated")),
            new EventEnvelope("new_name", new EventMetadata("TestRenamed")),
        };

        await eventStore.WriteAsync(streamId, events, StreamState.None);

        WatchResult? result = await eventStore.WatchAsync(null, TimeSpan.FromSeconds(5));

        result.Should()
              .NotBeNull();

        result!.StreamId.Should()
              .Be(streamId);

        result.StreamPosition.Should()
              .Be(1);

        WatchResult lastResult = result;

        result = await eventStore.WatchAsync(result.ContinuationToken, TimeSpan.FromSeconds(5));

        result.Should()
              .BeNull();

        await eventStore.WriteAsync(streamId + "-2", events, StreamState.None);
        await eventStore.WriteAsync(streamId, events, StreamState.Position(1));

        result = await eventStore.WatchAsync(lastResult.ContinuationToken, TimeSpan.FromSeconds(5));
        lastResult = result!;

        result = await eventStore.WatchAsync(lastResult.ContinuationToken, TimeSpan.FromSeconds(5));
    }
}