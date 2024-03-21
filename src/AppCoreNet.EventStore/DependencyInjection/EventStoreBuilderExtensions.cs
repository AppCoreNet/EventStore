// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to the <see cref="IEventStoreBuilder"/> interface.
/// </summary>
public static class EventStoreBuilderExtensions
{
    /// <summary>
    /// Configures the JSON serializer for the event store.
    /// </summary>
    /// <param name="builder">The <see cref="IEventStoreBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="JsonEventStoreSerializerOptions"/>.</param>
    /// <returns>The passed <see cref="IEventStoreBuilder"/> to allow chaining.</returns>
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