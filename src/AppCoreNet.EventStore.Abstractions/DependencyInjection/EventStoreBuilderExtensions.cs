// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to the <see cref="IEventStoreBuilder"/> interface.
/// </summary>
public static class EventStoreBuilderExtensions
{
    /// <summary>
    /// Configures event store subscriptions.
    /// </summary>
    /// <param name="builder">The <see cref="IEventStoreBuilder"/>.</param>
    /// <param name="configureOptions">An optional delegate to configure the <see cref="SubscriptionOptions"/>.</param>
    /// <returns>The passed <see cref="IEventStoreBuilder"/> to allow chaining.</returns>
    public static IEventStoreBuilder AddSubscriptions(
        this IEventStoreBuilder builder,
        Action<SubscriptionOptions>? configureOptions = null)
    {
        Ensure.Arg.NotNull(builder);

        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}