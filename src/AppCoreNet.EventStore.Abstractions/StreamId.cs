using System;
using System.Diagnostics;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

[DebuggerDisplay("{Value}")]
public sealed class StreamId : IEquatable<StreamId>
{
    private readonly string _id;

    public static readonly StreamId All = new ("$all");

    public string Value => _id;

    public bool IsPrefix => _id.EndsWith("*");

    public bool IsSuffix => _id.StartsWith("*");

    public bool IsWildcard => IsPrefix || IsSuffix || this == All;

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

    public override string ToString()
    {
        return _id;
    }

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

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is StreamId other && Equals(other));
    }

    public override int GetHashCode()
    {
        return StringComparer.InvariantCulture.GetHashCode(_id);
    }

    public static bool operator ==(StreamId? left, StreamId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StreamId? left, StreamId? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator StreamId(string id) => new (id);
}