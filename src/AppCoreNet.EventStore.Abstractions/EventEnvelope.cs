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
    public string EventType { get; }

    /// <summary>
    /// Gets the event data.
    /// </summary>
    public object Data { get; }

    /// <summary>
    /// Gets the <see cref="EventStore"/>.
    /// </summary>
    public EventMetadata Metadata { get; }

    public EventEnvelope(object data)
        : this(GetEventType(data), data)
    {
    }

    public EventEnvelope(string eventType, object data)
        : this(eventType, data, new EventMetadata())
    {
    }

    public EventEnvelope(string eventType, object data, EventMetadata metadata)
    {
        Ensure.Arg.NotEmpty(eventType);
        Ensure.Arg.NotNull(data);
        Ensure.Arg.NotNull(metadata);

        EventType = eventType;
        Data = data;
        Metadata = metadata;
    }

    private static string GetEventType(object data)
    {
        Ensure.Arg.NotNull(data);

        Type dataType = data.GetType();

        EventTypeAttribute? eventTypeAttribute =
            dataType.GetCustomAttributes<EventTypeAttribute>()
                    .FirstOrDefault();

        return eventTypeAttribute?.EventType ?? dataType.FullName!;
    }
}