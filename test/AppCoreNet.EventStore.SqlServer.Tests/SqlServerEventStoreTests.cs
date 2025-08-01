﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading.Tasks;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[Collection(SqlServerTestCollection.Name)]
[Trait("Category", "Integration")]
public class SqlServerEventStoreTests : EventStoreTests
{
    private readonly SqlServerTestFixture _sqlServerTestFixture;

    public SqlServerEventStoreTests(SqlServerTestFixture sqlServerTestFixture)
    {
        _sqlServerTestFixture = sqlServerTestFixture;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ApplicationName.Returns(typeof(SqlServerEventStoreTests).Assembly.FullName);
        services.TryAddSingleton(hostEnvironment);

        services.TryAddSingleton(Substitute.For<IEntityMapper>());

        services.AddDataProvider(
            p =>
            {
                p.AddEntityFrameworkCore<TestDbContext>(
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