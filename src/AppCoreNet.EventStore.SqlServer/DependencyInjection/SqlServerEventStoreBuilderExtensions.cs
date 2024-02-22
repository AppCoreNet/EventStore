using System;
using AppCoreNet.Data;
using AppCoreNet.Data.EntityFrameworkCore;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore;
using AppCoreNet.EventStore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

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

        Func<IServiceProvider, SqlServerSubscriptionManager<TDbContext>> subscriptionManagerFactory = sp =>
        {
            var dataProviderResolver = sp.GetRequiredService<IDataProviderResolver>();
            var dataProvider = (DbContextDataProvider<TDbContext>)dataProviderResolver.Resolve(dataProviderName);
            return ActivatorUtilities.CreateInstance<SqlServerSubscriptionManager<TDbContext>>(sp, dataProvider);
        };

        services.TryAdd(
        [
            ServiceDescriptor.Describe(typeof(SqlServerEventStore<TDbContext>), eventStoreFactory, lifetime),
            ServiceDescriptor.Describe(typeof(SqlServerSubscriptionManager<TDbContext>), subscriptionManagerFactory, lifetime),
        ]);

        services.TryAddEnumerable(
        [
            ServiceDescriptor.Transient<IEventStore, SqlServerEventStore<TDbContext>>(),
            ServiceDescriptor.Transient<ISubscriptionManager, SqlServerSubscriptionManager<TDbContext>>(),
        ]);

        if (configureOptions != null)
        {
            services.AddOptions<SqlServerEventStoreOptions>()
                    .Configure<IServiceProvider>((o, sp) => configureOptions(sp, o));
        }
    }

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

    public static DbContextDataProviderBuilder<TDbContext> AddEventStore<TDbContext>(
        this DbContextDataProviderBuilder<TDbContext> builder,
        Action<IServiceProvider, SqlServerEventStoreOptions>? configureOptions = null)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(builder);
        builder.Services.AddEventStore();
        builder.Services.AddSqlServer<TDbContext>(builder.Name, configureOptions, builder.ProviderLifetime);
        return builder;
    }

    public static DbContextDataProviderBuilder<TDbContext> AddEventStore<TDbContext>(
        this DbContextDataProviderBuilder<TDbContext> builder,
        Action<SqlServerEventStoreOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.Arg.NotNull(configureOptions);
        return builder.AddEventStore((_, o) => configureOptions(o));
    }
}