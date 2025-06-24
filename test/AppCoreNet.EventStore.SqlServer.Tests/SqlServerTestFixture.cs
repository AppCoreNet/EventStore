// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

public class SqlServerTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

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