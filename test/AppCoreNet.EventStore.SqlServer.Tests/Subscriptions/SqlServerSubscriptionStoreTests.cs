// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.EventStore.Subscription;
using AppCoreNet.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer.Subscription;

[Collection(SqlServerTestCollection.Name)]
[Trait("Category", "Integration")]
public class SqlServerSubscriptionStoreTests : SubscriptionStoreTests
{
    private readonly SqlServerTestFixture _sqlServerTestFixture;

    public SqlServerSubscriptionStoreTests(SqlServerTestFixture sqlServerTestFixture)
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
                 .AddSqlServerEventStore(o => o.SchemaName = "events");
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
}