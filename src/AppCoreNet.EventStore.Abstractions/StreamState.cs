// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Diagnostics;
using System.Globalization;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Specifies the expected state of a stream.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct StreamState : IFormattable, IEquatable<StreamState>
{
    private const long AnyValue = -1;
    private const long NoneValue = -2;

    /// <summary>
    /// Specifies that the stream can be at any position.
    /// </summary>
    public static readonly StreamState Any = new (AnyValue);

    /// <summary>
    /// Specifies that the stream should not exist.
    /// </summary>
    public static readonly StreamState None = new (NoneValue);

    /// <summary>
    /// Gets the value.
    /// </summary>
    public long Value { get; }

    private StreamState(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an instance of <see cref="StreamPosition"/> with the provided event index.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The expected event index.</returns>
    public static StreamState Index(long value)
    {
        Ensure.Arg.InRange(value, 0, long.MaxValue);
        return new StreamState(value);
    }

    /// <inheritdoc />
    public bool Equals(StreamState other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is StreamState other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The stream state represented as a string.</returns>
    public override string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider formatProvider)
    {
        switch (Value)
        {
            case AnyValue:
                return "any";
            case NoneValue:
                return "none";
            default:
                return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Compares two <see cref="StreamState"/> instances for equality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamState"/>.</param>
    /// <param name="right">The second <see cref="StreamState"/>.</param>
    /// <returns><c>true</c> if both objects are equal; <c>false</c> otherwise.</returns>
    public static bool operator ==(StreamState left, StreamState right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="StreamState"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamState"/>.</param>
    /// <param name="right">The second <see cref="StreamState"/>.</param>
    /// <returns><c>true</c> if both objects are not equal; <c>false</c> otherwise.</returns>
    public static bool operator !=(StreamState left, StreamState right)
        => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a <c>long</c> to an instance of <see cref="StreamState"/>.
    /// </summary>
    /// <param name="index">The value.</param>
    /// <returns>The <see cref="StreamState"/>.</returns>
    public static implicit operator StreamState(long index) => Index(index);
}