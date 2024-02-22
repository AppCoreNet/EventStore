using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

public class SqlServerTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string
        ConnectionString => "Server=localhost;Database=eventstore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=false;Connect Timeout=30;";
        // _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}