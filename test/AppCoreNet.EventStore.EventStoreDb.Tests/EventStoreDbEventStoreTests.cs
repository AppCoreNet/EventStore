// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCoreNet.EventStore.EventStoreDb;

[Collection(EventStoreDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class EventStoreDbEventStoreTests : EventStoreTests
{
    private readonly EventStoreDbTestFixture _eventStoreDbTestFixture;

    public EventStoreDbEventStoreTests(EventStoreDbTestFixture eventStoreDbTestFixture)
    {
        _eventStoreDbTestFixture = eventStoreDbTestFixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddEventStoreClient(_eventStoreDbTestFixture.ConnectionString);

        services.ConfigureEventStore()
                .AddEventStoreDb();
    }
}