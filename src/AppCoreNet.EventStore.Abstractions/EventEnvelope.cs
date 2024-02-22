// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Linq;
using System.Reflection;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents an event with type name and metadata.
/// </summary>
public sealed class EventEnvelope : IEventEnvelope
{
    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    public string EventTypeName { get; }

    /// <summary>
    /// Gets the event data.
    /// </summary>
    public object Data { get; }

    /// <summary>
    /// Gets the <see cref="EventStore"/>.
    /// </summary>
    public EventMetadata Metadata { get; }

    public EventEnvelope(object data)
        : this(GetEventTypeName(data), data)
    {
    }

    public EventEnvelope(string eventTypeName, object data)
        : this(eventTypeName, data, new EventMetadata())
    {
    }

    public EventEnvelope(string eventTypeName, object data, EventMetadata metadata)
    {
        Ensure.Arg.NotEmpty(eventTypeName);
        Ensure.Arg.NotNull(data);
        Ensure.Arg.NotNull(metadata);

        EventTypeName = eventTypeName;
        Data = data;
        Metadata = metadata;
    }

    private static string GetEventTypeName(object data)
    {
        Ensure.Arg.NotNull(data);

        Type dataType = data.GetType();

        EventTypeAttribute? eventTypeAttribute =
            dataType.GetCustomAttributes<EventTypeAttribute>()
                    .FirstOrDefault();

        return eventTypeAttribute?.EventTypeName ?? dataType.FullName!;
    }
}