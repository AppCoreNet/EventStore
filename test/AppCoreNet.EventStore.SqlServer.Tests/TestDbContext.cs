// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.EventStore.SqlServer.Model;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyEventStoreConfiguration(schema: "events");
    }
}