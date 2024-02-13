// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Text.Json;
using AppCoreNet.Diagnostics;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.Serialization;

/// <summary>
/// Provides a JSON serializer for events.
/// </summary>
public class JsonEventStoreSerializer : IEventStoreSerializer
{
    private readonly JsonEventStoreSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonEventStoreSerializer"/> class.
    /// </summary>
    /// <param name="options">The <see cref="JsonEventStoreSerializerOptions"/>.</param>
    public JsonEventStoreSerializer(IOptions<JsonEventStoreSerializerOptions> options)
    {
        Ensure.Arg.NotNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public string? Serialize(object? obj)
    {
        return obj != null
            ? JsonSerializer.Serialize(obj, _options.JsonSerializerOptions)
            : null;
    }

    /// <inheritdoc />
    public object? Deserialize(string typeName, string? data)
    {
        if (data == null)
            return null;

        if (!_options.EventTypeMap.TryGetValue(typeName, out Type? type))
        {
            type = Type.GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException(
                    $"The type '{typeName}' cannot be deserialized because it was not found.");
            }
        }

        return JsonSerializer.Deserialize(data, type, _options.JsonSerializerOptions);
    }
}