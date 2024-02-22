using System;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

public static class EventStoreBuilderExtensions
{
    public static IEventStoreBuilder AddJsonSerializer(
        this IEventStoreBuilder builder,
        Action<JsonEventStoreSerializerOptions>? configureOptions = null)
    {
        Ensure.Arg.NotNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreSerializer, JsonEventStoreSerializer>());

        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}