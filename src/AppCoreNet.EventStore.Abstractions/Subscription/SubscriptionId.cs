// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Diagnostics;
using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore.Subscription;

/// <summary>
/// Represents a subscription ID.
/// </summary>
[DebuggerDisplay("{Value}")]
public sealed class SubscriptionId : IEquatable<SubscriptionId>
{
    private readonly string _id;

    /// <summary>
    /// Gets the special ID which refers to all existing subscriptions.
    /// </summary>
    public static readonly SubscriptionId All = new ("*");

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
    /// Initializes a new instance of the <see cref="SubscriptionId"/> class.
    /// </summary>
    /// <param name="id">The subscription ID, may start or end with wildcard (star) character.</param>
    public SubscriptionId(string id)
    {
        Ensure.Arg.NotEmpty(id);

        int firstStarIndex = id.IndexOf('*');
        int lastStarIndex = id.LastIndexOf('*');

        if ((firstStarIndex >= 0 && firstStarIndex != lastStarIndex)
            || (firstStarIndex >= 0 && firstStarIndex != lastStarIndex && lastStarIndex != id.Length - 1))
        {
            throw new ArgumentException("Subscription ID must not contain more than two wildcard characters.", nameof(id));
        }

        _id = id;
    }

    /// <summary>
    /// Creates a <see cref="SubscriptionId"/> instance which matches subscriptions by prefix.
    /// </summary>
    /// <param name="prefix">The prefix of the subscription ID.</param>
    /// <returns>A new <see cref="SubscriptionId"/> instance.</returns>
    public static SubscriptionId Prefix(string prefix)
    {
        Ensure.Arg.NotEmpty(prefix);
        return new ($"{prefix}*");
    }

    /// <summary>
    /// Creates a <see cref="SubscriptionId"/> instance which matches subscriptions by suffix.
    /// </summary>
    /// <param name="suffix">The suffix of the subscription ID.</param>
    /// <returns>A new <see cref="SubscriptionId"/> instance.</returns>
    public static SubscriptionId Suffix(string suffix)
    {
        Ensure.Arg.NotEmpty(suffix);
        return new ($"*{suffix}");
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
    public bool Equals(SubscriptionId? other)
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
        return ReferenceEquals(this, obj) || (obj is SubscriptionId other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.InvariantCulture.GetHashCode(_id);
    }

    /// <summary>
    /// Compares two <see cref="SubscriptionId"/> instances for equality.
    /// </summary>
    /// <param name="left">The first <see cref="SubscriptionId"/>.</param>
    /// <param name="right">The second <see cref="SubscriptionId"/>.</param>
    /// <returns><c>true</c> if both objects are equal; <c>false</c> otherwise.</returns>
    public static bool operator ==(SubscriptionId? left, SubscriptionId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two <see cref="SubscriptionId"/> instances for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="SubscriptionId"/>.</param>
    /// <param name="right">The second <see cref="SubscriptionId"/>.</param>
    /// <returns><c>true</c> if both objects are not equal; <c>false</c> otherwise.</returns>
    public static bool operator !=(SubscriptionId? left, SubscriptionId? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Implicitly converts a <c>string</c> to an instance of <see cref="SubscriptionId"/>.
    /// </summary>
    /// <param name="id">The value.</param>
    /// <returns>The <see cref="SubscriptionId"/>.</returns>
    public static implicit operator SubscriptionId(string id) => new (id);
}