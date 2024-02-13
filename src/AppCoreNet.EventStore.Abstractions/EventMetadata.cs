using System;
using System.Collections.Generic;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public sealed class EventMetadata
{
    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the stream position of the event.
    /// </summary>
    public long Position { get; init; }

    /// <summary>
    /// Gets the creation time of the event.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets the dictionary of additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventMetadata"/> class.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    public EventMetadata(string eventType)
    {
        Ensure.Arg.NotEmpty(eventType);
        EventType = eventType;
    }
}