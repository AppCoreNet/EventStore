// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

internal sealed class EventStoreBuilder : IEventStoreBuilder
{
    public IServiceCollection Services { get; }

    public EventStoreBuilder(IServiceCollection services)
    {
        Services = services;
    }
}