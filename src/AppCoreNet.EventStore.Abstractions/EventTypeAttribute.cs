﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Specifies the type name of an event.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class EventTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    public string EventTypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeAttribute"/> class.
    /// </summary>
    /// <param name="eventTypeName">The type name of the event.</param>
    public EventTypeAttribute(string eventTypeName)
    {
        Ensure.Arg.NotEmpty(eventTypeName);
        EventTypeName = eventTypeName;
    }
}