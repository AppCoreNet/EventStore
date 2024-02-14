// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore;

/// <summary>
/// Specifies the direction when reading from a stream.
/// </summary>
public enum StreamReadDirection
{
    /// <summary>
    /// Events are read in the order they were persisted (oldest first).
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Events are read in the opposite order they were persisted (newest first).
    /// </summary>
    Backward = 1,
}