// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Serialization;

/// <summary>
/// Provides options for the <see cref="JsonEventStoreSerializer"/>.
/// </summary>
public class JsonEventStoreSerializerOptions
{
    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets the type name map.
    /// </summary>
    public IDictionary<string, Type> TypeNameMap { get; } = new Dictionary<string, Type>
    {
        { "StringDictionary", typeof(Dictionary<string, string>) },
    };

    /// <summary>
    /// Adds a type name mapping for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type for which to add the mapping.</param>
    /// <returns>The <see cref="JsonEventStoreSerializerOptions"/> to allow chaining.</returns>
    public JsonEventStoreSerializerOptions AddTypeMap(Type type)
    {
        TypeNameMap.Add(GetEventTypeName(type), type);
        return this;
    }

    /// <summary>
    /// Adds a type name mapping for the specified <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type for which to add the mapping.</typeparam>
    /// <returns>The <see cref="JsonEventStoreSerializerOptions"/> to allow chaining.</returns>
    public JsonEventStoreSerializerOptions AddTypeMap<T>()
    {
        return AddTypeMap(typeof(T));
    }

    /// <summary>
    /// Adds type name mappings for all types in the specified <paramref name="assembly"/>.
    /// </summary>
    /// <remarks>
    /// Adds mappings for all types which are decorated with <see cref="EventTypeAttribute"/>.
    /// </remarks>
    /// <param name="assembly">The assembly to search.</param>
    /// <returns>The <see cref="JsonEventStoreSerializerOptions"/> to allow chaining.</returns>
    public JsonEventStoreSerializerOptions AddTypeMapsFrom(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            var eventTypeAttribute = type.GetCustomAttribute<EventTypeAttribute>();
            if (eventTypeAttribute != null)
            {
                TypeNameMap.Add(eventTypeAttribute.EventTypeName, type);
            }
        }

        return this;
    }

    private static string GetEventTypeName(Type dataType)
    {
        Ensure.Arg.NotNull(dataType);

        var eventTypeAttribute = dataType.GetCustomAttribute<EventTypeAttribute>();
        return eventTypeAttribute?.EventTypeName ?? dataType.FullName!;
    }
}