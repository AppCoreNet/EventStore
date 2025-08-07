// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading.Tasks;
using Testcontainers.EventStoreDb;
using Xunit;

namespace AppCoreNet.EventStore.EventStoreDb;

public class EventStoreDbTestFixture : IAsyncLifetime
{
    private readonly EventStoreDbContainer _container = new EventStoreDbBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}