// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore;
using AppCoreNet.EventStore.EventStoreDb;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register Event Store DB event store with the DI container.
/// </summary>
public static class EventStoreDbEventStoreBuilderExtensions
{
    public static IEventStoreBuilder AddEventStoreDb(
        this IEventStoreBuilder builder,
        Action<EventStoreDbOptions>? configure = null)
    {
        Ensure.Arg.NotNull(builder);

        builder.Services.TryAddSingleton<IEventStore, EventStoreDbEventStore>();
        return builder;
    }
}