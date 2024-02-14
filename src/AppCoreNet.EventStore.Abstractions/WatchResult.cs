// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents the result of <see cref="IEventStore.WatchAsync"/>.
/// </summary>
public sealed class WatchResult
{
    /// <summary>
    /// Gets the position of the last observed event in the watched stream.
    /// </summary>
    /// <remarks>
    /// The position refers to the <see cref="EventMetadata.StreamPosition"/> when watch was invoked
    /// for a specific stream. If watch was invoked for the $all stream it refers to the
    /// <see cref="EventMetadata.GlobalPosition"/>.
    /// </remarks>
    public long Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchResult"/> class.
    /// </summary>
    /// <param name="position">The position of the last observed event.</param>
    public WatchResult(long position)
    {
        Ensure.Arg.InRange(position, 0, long.MaxValue);
        Position = position;
    }
}