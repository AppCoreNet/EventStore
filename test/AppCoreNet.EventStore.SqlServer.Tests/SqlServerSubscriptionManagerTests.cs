using System;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.EventStore.Serialization;
using AppCoreNet.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[Collection(SqlServerTestCollection.Name)]
[Trait("Category", "Integration")]
public class SqlServerSubscriptionManagerTests : SubscriptionManagerTests, IAsyncLifetime
{
    private readonly SqlServerTestFixture _sqlServerTestFixture;

    public SqlServerSubscriptionManagerTests(SqlServerTestFixture sqlServerTestFixture)
    {
        _sqlServerTestFixture = sqlServerTestFixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.TryAddSingleton(Substitute.For<IEntityMapper>());

        services.AddDataProvider(
            p =>
            {
                p.AddDbContext<TestDbContext>(
                     o =>
                     {
                         o.UseSqlServer(_sqlServerTestFixture.ConnectionString);
                     })
                 .AddEventStore(o => o.SchemaName = "events");
            });
    }

    public async Task InitializeAsync()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<DbContextDataProvider<TestDbContext>>();
        await provider.DbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task ProcessSubscriptionsAsync(ISubscriptionManager manager)
    {
        await ((SqlServerSubscriptionManager<TestDbContext>)manager).ProcessAsync(TimeSpan.FromSeconds(5));
    }
}