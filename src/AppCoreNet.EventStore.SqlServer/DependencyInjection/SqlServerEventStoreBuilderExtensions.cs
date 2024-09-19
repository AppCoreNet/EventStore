// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore;
using AppCoreNet.EventStore.SqlServer;
using AppCoreNet.EventStore.SqlServer.Subscriptions;
using AppCoreNet.EventStore.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register SQL Server event store with the DI container.
/// </summary>
public static class SqlServerEventStoreBuilderExtensions
{
    private static void AddSqlServer<TDbContext>(
        this IServiceCollection services,
        string dataProviderName,
        Action<IServiceProvider, SqlServerEventStoreOptions>? configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        Func<IServiceProvider, SqlServerEventStore<TDbContext>> eventStoreFactory = sp =>
        {
            var dataProviderResolver = sp.GetRequiredService<IDataProviderResolver>();
            var dataProvider = (DbContextDataProvider<TDbContext>)dataProviderResolver.Resolve(dataProviderName);
            return ActivatorUtilities.CreateInstance<SqlServerEventStore<TDbContext>>(sp, dataProvider);
        };

        Func<IServiceProvider, SqlServerSubscriptionStore<TDbContext>> subscriptionManagerFactory = sp =>
        {
            var dataProviderResolver = sp.GetRequiredService<IDataProviderResolver>();
            var dataProvider = (DbContextDataProvider<TDbContext>)dataProviderResolver.Resolve(dataProviderName);
            return ActivatorUtilities.CreateInstance<SqlServerSubscriptionStore<TDbContext>>(sp, dataProvider);
        };

        services.TryAdd(
        [
            ServiceDescriptor.Describe(typeof(SqlServerEventStore<TDbContext>), eventStoreFactory, lifetime),
            ServiceDescriptor.Describe(typeof(SqlServerSubscriptionStore<TDbContext>), subscriptionManagerFactory, lifetime),
        ]);

        services.TryAddEnumerable(
        [
            ServiceDescriptor.Transient<IPostConfigureOptions<SqlServerEventStoreOptions>, SqlServerConfigureEventStoreOptions>(),
            ServiceDescriptor.Transient<IEventStore, SqlServerEventStore<TDbContext>>(),
            ServiceDescriptor.Transient<ISubscriptionStore, SqlServerSubscriptionStore<TDbContext>>(),
        ]);

        if (configureOptions != null)
        {
            services.AddOptions<SqlServerEventStoreOptions>()
                    .Configure<IServiceProvider>((o, sp) => configureOptions(sp, o));
        }
    }

    /// <summary>
    /// Registers the SQL server based event store with the DI container.
    /// </summary>
    /// <param name="builder">The <see cref="IEventStoreBuilder"/>.</param>
    /// <param name="dataProviderName">The name of the SQL server data provider.</param>
    /// <param name="configureOptions">Delegate used to configure the SQL server event store.</param>
    /// <param name="lifetime">The lifetime of the <see cref="DbContext"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="IEventStoreBuilder"/> which allows chaining of method calls.</returns>
    public static IEventStoreBuilder AddSqlServer<TDbContext>(
        this IEventStoreBuilder builder,
        string dataProviderName,
        Action<IServiceProvider, SqlServerEventStoreOptions>? configureOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(builder);
        builder.Services.AddSqlServer<TDbContext>(dataProviderName, configureOptions, lifetime);
        return builder;
    }

    /// <summary>
    /// Registers the SQL server based event store with the DI container.
    /// </summary>
    /// <param name="builder">The <see cref="IEventStoreBuilder"/>.</param>
    /// <param name="dataProviderName">The name of the SQL server data provider.</param>
    /// <param name="configureOptions">Delegate used to configure the SQL server event store.</param>
    /// <param name="lifetime">The lifetime of the <see cref="DbContext"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="IEventStoreBuilder"/> which allows chaining of method calls.</returns>
    public static IEventStoreBuilder AddSqlServer<TDbContext>(
        this IEventStoreBuilder builder,
        string dataProviderName,
        Action<SqlServerEventStoreOptions> configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(configureOptions);
        return builder.AddSqlServer<TDbContext>(dataProviderName, (_, o) => configureOptions(o), lifetime);
    }

    /// <summary>
    /// Registers the SQL server based event store for the data provider.
    /// </summary>
    /// <param name="builder">The <see cref="DbContextDataProviderBuilder{TDbContext}"/>.</param>
    /// <param name="configureOptions">Delegate used to configure the SQL server event store.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="DbContextDataProviderBuilder{TDbContext}"/> which allows chaining of method calls.</returns>
    public static DbContextDataProviderBuilder<TDbContext> AddSqlServerEventStore<TDbContext>(
        this DbContextDataProviderBuilder<TDbContext> builder,
        Action<IServiceProvider, SqlServerEventStoreOptions>? configureOptions = null)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(builder);
        builder.Services.AddEventStore();
        builder.Services.AddSqlServer<TDbContext>(builder.Name, configureOptions, builder.ProviderLifetime);
        return builder;
    }

    /// <summary>
    /// Registers the SQL server based event store for the data provider.
    /// </summary>
    /// <param name="builder">The <see cref="DbContextDataProviderBuilder{TDbContext}"/>.</param>
    /// <param name="configureOptions">Delegate used to configure the SQL server event store.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="DbContextDataProviderBuilder{TDbContext}"/> which allows chaining of method calls.</returns>
    public static DbContextDataProviderBuilder<TDbContext> AddSqlServerEventStore<TDbContext>(
        this DbContextDataProviderBuilder<TDbContext> builder,
        Action<SqlServerEventStoreOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(configureOptions);
        return builder.AddSqlServerEventStore((_, o) => configureOptions(o));
    }
}