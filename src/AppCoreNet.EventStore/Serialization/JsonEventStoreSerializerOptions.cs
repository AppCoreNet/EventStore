// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Text.Json;

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
}