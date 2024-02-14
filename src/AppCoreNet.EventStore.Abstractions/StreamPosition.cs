// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Globalization;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Specifies the position of a stream when reading or watching for new events.
/// </summary>
public readonly struct StreamPosition : IFormattable, IEquatable<StreamPosition>
{
    private const long StartValue = -1;
    private const long EndValue = -2;

    /// <summary>
    /// Specifies to read from the start of the stream.
    /// </summary>
    public static readonly StreamPosition Start = new (StartValue);

    /// <summary>
    /// Specifies to read from the end of the stream.
    /// </summary>
    public static readonly StreamPosition End = new (EndValue);

    /// <summary>
    /// Gets the value.
    /// </summary>
    public long Value { get; }

    private StreamPosition(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an instance of <see cref="StreamPosition"/> with the provided <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The stream position.</returns>
    public static StreamPosition FromValue(long value)
    {
        Ensure.Arg.InRange(value, 0, long.MaxValue);
        return new StreamPosition(value);
    }

    /// <inheritdoc />
    public bool Equals(StreamPosition other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is StreamPosition other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider formatProvider)
    {
        switch (Value)
        {
            case StartValue:
                return "start";
            case EndValue:
                return "end";
            default:
                return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Compares two <see cref="StreamPosition"/> instances for equality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamPosition"/>.</param>
    /// <param name="right">The second <see cref="StreamPosition"/>.</param>
    /// <returns><c>true</c> if both objects are equal; <c>false</c> otherwise.</returns>
    public static bool operator ==(StreamPosition left, StreamPosition right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="StreamPosition"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamPosition"/>.</param>
    /// <param name="right">The second <see cref="StreamPosition"/>.</param>
    /// <returns><c>true</c> if both objects are not equal; <c>false</c> otherwise.</returns>
    public static bool operator !=(StreamPosition left, StreamPosition right)
        => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a <c>long</c> to an instance of <see cref="StreamPosition"/>.
    /// </summary>
    /// <param name="position">The value.</param>
    /// <returns>The <see cref="StreamPosition"/>.</returns>
    public static implicit operator StreamPosition(long position) => FromValue(position);
}