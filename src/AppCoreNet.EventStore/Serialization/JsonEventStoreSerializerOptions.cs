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
        TypeNameMap[GetEventTypeName(type)] = type;
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

    private static string GetEventTypeName(Type dataType)
    {
        Ensure.Arg.NotNull(dataType);

        EventTypeAttribute? eventTypeAttribute =
            dataType.GetCustomAttributes<EventTypeAttribute>()
                    .FirstOrDefault();

        return eventTypeAttribute?.EventTypeName ?? dataType.FullName!;
    }
}