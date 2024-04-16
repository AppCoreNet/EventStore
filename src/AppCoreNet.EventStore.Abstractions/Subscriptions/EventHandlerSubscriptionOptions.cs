// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppCoreNet.EventStore.Subscriptions;

/// <summary>
/// Provides options for event handler subscriptions.
/// </summary>
public sealed class EventHandlerSubscriptionOptions
{
    internal List<Func<IServiceProvider, IEventHandler>> EventHandlerFactories { get; } = new ();

    /// <summary>
    /// Adds the specified factory to create an <see cref="IEventHandler"/>.
    /// </summary>
    /// <param name="eventHandlerFactory">The event handler factory.</param>
    /// <returns>The <see cref="EventHandlerSubscriptionOptions"/> instance to allow chaining.</returns>
    public EventHandlerSubscriptionOptions Add(Func<IServiceProvider, IEventHandler> eventHandlerFactory)
    {
        Ensure.Arg.NotNull(eventHandlerFactory);
        EventHandlerFactories.Add(eventHandlerFactory);
        return this;
    }

    /// <summary>
    /// Adds the specified <see cref="IEventHandler"/> instance.
    /// </summary>
    /// <param name="eventHandler">The <see cref="IEventHandler"/>.</param>
    /// <returns>The <see cref="EventHandlerSubscriptionOptions"/> instance to allow chaining.</returns>
    public EventHandlerSubscriptionOptions Add(IEventHandler eventHandler)
    {
        Ensure.Arg.NotNull(eventHandler);
        return Add(_ => eventHandler);
    }

    /// <summary>
    /// Adds the specified <see cref="IEventHandler"/> type.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="IEventHandler"/>.</typeparam>
    /// <returns>The <see cref="EventHandlerSubscriptionOptions"/> instance to allow chaining.</returns>
    public EventHandlerSubscriptionOptions Add<T>()
        where T : IEventHandler
    {
        return Add(sp => ActivatorUtilities.GetServiceOrCreateInstance<T>(sp));
    }

    /// <summary>
    /// Adds the specified <see cref="IEventHandler"/> type.
    /// </summary>
    /// <param name="eventHandlerType">The type of the <see cref="IEventHandler"/>.</param>
    /// <returns>The <see cref="EventHandlerSubscriptionOptions"/> instance to allow chaining.</returns>
    public EventHandlerSubscriptionOptions Add(Type eventHandlerType)
    {
        Ensure.Arg.NotNull(eventHandlerType);
        Ensure.Arg.OfType(eventHandlerType, typeof(IEventHandler));
        return Add(sp => (IEventHandler)ActivatorUtilities.GetServiceOrCreateInstance(sp, eventHandlerType));
    }

    /// <summary>
    /// Adds all <see cref="IEventHandler"/> types found in the specified assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/>.</param>
    /// <returns>The <see cref="EventHandlerSubscriptionOptions"/> instance to allow chaining.</returns>
    public EventHandlerSubscriptionOptions AddFromAssembly(Assembly assembly)
    {
        Ensure.Arg.NotNull(assembly);

        IEnumerable<Type> eventHandlerTypes =
            assembly.GetTypes()
                    .Where(
                        t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                             && typeof(IEventHandler).IsAssignableFrom(t));

        foreach (Type eventHandlerType in eventHandlerTypes)
        {
            Add(eventHandlerType);
        }

        return this;
    }
}