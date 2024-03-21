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
public sealed class EventEnvelope
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

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class.
    /// </summary>
    /// <remarks>
    /// The event type name can be specified by the <see cref="EventTypeAttribute"/>.
    /// </remarks>
    /// <param name="data">The event data.</param>
    public EventEnvelope(object data)
        : this(GetEventTypeName(data), data)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class.
    /// </summary>
    /// <param name="eventTypeName">The event type name.</param>
    /// <param name="data">The event data.</param>
    public EventEnvelope(string eventTypeName, object data)
        : this(eventTypeName, data, new EventMetadata())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class.
    /// </summary>
    /// <param name="eventTypeName">The event type name.</param>
    /// <param name="data">The event data.</param>
    /// <param name="metadata">The event metadata.</param>
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