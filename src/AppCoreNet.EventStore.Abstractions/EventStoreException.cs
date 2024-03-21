// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;

namespace AppCoreNet.EventStore;

/// <summary>
/// Thrown when there was some error accessing the event store.
/// </summary>
public class EventStoreException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EventStoreException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventStoreException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}