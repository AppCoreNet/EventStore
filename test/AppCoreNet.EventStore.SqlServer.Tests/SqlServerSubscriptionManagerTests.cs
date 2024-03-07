using System;
using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[Collection(SqlServerTestCollection.Name)]
[Trait("Category", "Integration")]
public class SqlServerSubscriptionManagerTests : SubscriptionManagerTests
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

    protected override async Task InitializeAsync()
    {
        await using ServiceProvider sp = CreateServiceProvider();
        await using AsyncServiceScope scope = sp.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<DbContextDataProvider<TestDbContext>>();
        await provider.DbContext.Database.MigrateAsync();

        await base.InitializeAsync();
    }

    protected override async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override async Task<IDisposable> BeginTransaction(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetRequiredService<DbContextDataProvider<TestDbContext>>();
        return await provider.TransactionManager.BeginTransactionAsync();
    }

    protected override async Task CommitTransaction(IDisposable transaction)
    {
        await ((ITransaction)transaction).CommitAsync();
    }
}