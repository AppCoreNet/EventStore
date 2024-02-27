using System;
using System.Diagnostics;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

/// <summary>
/// Represents a stream ID.
/// </summary>
[DebuggerDisplay("{Value}")]
public sealed class StreamId : IEquatable<StreamId>
{
    private readonly string _id;

    /// <summary>
    /// Gets the special ID which refers to all existing streams.
    /// </summary>
    public static readonly StreamId All = new ("*");

    /// <summary>
    /// Gets the ID value.
    /// </summary>
    public string Value => _id;

    /// <summary>
    /// Gets a value indicating whether the value is a prefix.
    /// </summary>
    public bool IsPrefix => _id.Length > 1 && _id.EndsWith("*");

    /// <summary>
    /// Gets a value indicating whether the value is a suffix.
    /// </summary>
    public bool IsSuffix => _id.Length > 1 && _id.StartsWith("*");

    /// <summary>
    /// Gets a value indicating whether the value contains a wildcard.
    /// </summary>
    public bool IsWildcard => _id.Contains("*");

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamId"/> class.
    /// </summary>
    /// <param name="id">The stream ID, may contain wildcard (star) characters.</param>
    public StreamId(string id)
    {
        Ensure.Arg.NotEmpty(id);

        // TODO: validate that '*' is only present once and only as first or last character
        _id = id;
    }

    public static StreamId Prefix(string id)
    {
        Ensure.Arg.NotEmpty(id);
        return new ($"{id}*");
    }

    public static StreamId Suffix(string id)
    {
        Ensure.Arg.NotEmpty(id);
        return new ($"*{id}");
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The ID represented as a string.</returns>
    public override string ToString()
    {
        return _id;
    }

    /// <inheritdoc />
    public bool Equals(StreamId? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(_id, other._id, StringComparison.InvariantCulture);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is StreamId other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.InvariantCulture.GetHashCode(_id);
    }

    /// <summary>
    /// Compares two <see cref="StreamId"/> instances for equality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamId"/>.</param>
    /// <param name="right">The second <see cref="StreamId"/>.</param>
    /// <returns><c>true</c> if both objects are equal; <c>false</c> otherwise.</returns>
    public static bool operator ==(StreamId? left, StreamId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two <see cref="StreamId"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="StreamId"/>.</param>
    /// <param name="right">The second <see cref="StreamId"/>.</param>
    /// <returns><c>true</c> if both objects are not equal; <c>false</c> otherwise.</returns>
    public static bool operator !=(StreamId? left, StreamId? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Implicitly converts a <c>string</c> to an instance of <see cref="StreamId"/>.
    /// </summary>
    /// <param name="id">The value.</param>
    /// <returns>The <see cref="StreamId"/>.</returns>
    public static implicit operator StreamId(string id) => new (id);
}