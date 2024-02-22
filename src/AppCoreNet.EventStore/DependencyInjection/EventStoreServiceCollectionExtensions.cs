using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

public static class EventStoreServiceCollectionExtensions
{
    public static IEventStoreBuilder AddEventStore(this IServiceCollection services)
    {
        Ensure.Arg.NotNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreSerializer, JsonEventStoreSerializer>());

        return new EventStoreBuilder(services);
    }
}