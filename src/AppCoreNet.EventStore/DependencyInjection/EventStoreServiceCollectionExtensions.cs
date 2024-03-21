// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register the Event Store.
/// </summary>
public static class EventStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers Event Store services with the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IEventStoreBuilder"/> to configure the Event Store.</returns>
    public static IEventStoreBuilder AddEventStore(this IServiceCollection services)
    {
        Ensure.Arg.NotNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreSerializer, JsonEventStoreSerializer>());

        return new EventStoreBuilder(services);
    }
}