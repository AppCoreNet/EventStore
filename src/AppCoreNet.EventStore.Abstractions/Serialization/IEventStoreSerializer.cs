// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.Serialization;

/// <summary>
/// Represents a serializer for events.
/// </summary>
public interface IEventStoreSerializer
{
    /// <summary>
    /// Serializes the specified object.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The serialized object.</returns>
    string? Serialize(object? obj);

    /// <summary>
    /// Deserializes the specified data.
    /// </summary>
    /// <param name="typeName">The name of the type to deserialize.</param>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized object.</returns>
    object? Deserialize(string typeName, string? data);
}