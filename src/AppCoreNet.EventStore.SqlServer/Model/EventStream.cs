// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class EventStream
{
    /// <summary>
    /// Gets the internal ID of the stream.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the ID of the stream.
    /// </summary>
    required public string StreamId { get; init; }

    /// <summary>
    /// Gets the last sequence of the stream.
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// Gets the current position of the stream.
    /// </summary>
    public long Position { get; init; }
}