// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents the result of <see cref="IEventStore.WatchAsync"/>.
/// </summary>
public sealed class WatchEventResult
{
    /// <summary>
    /// Gets the position of the last observed event in the watched stream.
    /// </summary>
    /// <remarks>
    /// The position refers to the <see cref="EventMetadata.StreamPosition"/> when watch was invoked
    /// for a specific stream. If watch was invoked for a wildcard stream it refers to the
    /// <see cref="EventMetadata.GlobalPosition"/>.
    /// </remarks>
    public long Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchEventResult"/> class.
    /// </summary>
    /// <param name="position">The position of the last observed event.</param>
    public WatchEventResult(long position)
    {
        Ensure.Arg.InRange(position, 0, long.MaxValue);
        Position = position;
    }
}