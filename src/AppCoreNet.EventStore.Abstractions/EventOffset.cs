// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents the offset of an event in a store.
/// </summary>
public readonly struct EventOffset : IEquatable<EventOffset>
{
    /// <summary>
    /// Gets the offset value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Specifies the first event.
    /// </summary>
    public static readonly EventOffset Start = new EventOffset(-1);

    /// <summary>
    /// Specifies the first uncommitted event.
    /// </summary>
    public static readonly EventOffset Next = new EventOffset(-2);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventOffset"/> struct.
    /// </summary>
    /// <param name="value">The event offset.</param>
    public EventOffset(long value)
    {
        Ensure.Arg.InRange(value, -2, long.MaxValue);
        Value = value;
    }

    /// <summary>
    /// Implicitly converts a <see cref="long"/> to a <see cref="EventOffset"/>.
    /// </summary>
    /// <param name="value">The event offset.</param>
    public static implicit operator EventOffset(long value)
    {
        return new EventOffset(value);
    }

    /// <inheritdoc />
    public bool Equals(EventOffset other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is EventOffset other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Compares two <see cref="EventOffset"/> instances for equality.
    /// </summary>
    /// <param name="left">The first <see cref="EventOffset"/>.</param>
    /// <param name="right">The second <see cref="EventOffset"/>.</param>
    /// <returns><c>true</c> if both objects are equal; <c>false</c> otherwise.</returns>
    public static bool operator ==(EventOffset left, EventOffset right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="EventOffset"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="EventOffset"/>.</param>
    /// <param name="right">The second <see cref="EventOffset"/>.</param>
    /// <returns><c>true</c> if both objects are not equal; <c>false</c> otherwise.</returns>
    public static bool operator !=(EventOffset left, EventOffset right)
    {
        return !left.Equals(right);
    }
}