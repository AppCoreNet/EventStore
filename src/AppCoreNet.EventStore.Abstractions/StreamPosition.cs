using System;
using System.Globalization;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

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

    public static StreamPosition Position(long value)
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
            case EndValue:
                return "end";
            default:
                return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static bool operator ==(StreamPosition left, StreamPosition right)
        => left.Equals(right);

    public static bool operator !=(StreamPosition left, StreamPosition right)
        => !left.Equals(right);

    public static implicit operator StreamPosition(long position) => Position(position);
}