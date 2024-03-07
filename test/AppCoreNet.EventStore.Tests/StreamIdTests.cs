// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using FluentAssertions;
using Xunit;

namespace AppCoreNet.EventStore;

public class StreamIdTests
{
    [Fact]
    public void All()
    {
        var id = new StreamId("*");

        id.Value.Should()
          .Be("*");

        id.IsWildcard.Should()
          .BeTrue();

        id.IsPrefix.Should()
          .BeFalse();

        id.IsSuffix.Should()
          .BeFalse();
    }

    [Fact]
    public void Prefix()
    {
        var id = new StreamId("a*");

        id.Value.Should()
          .Be("a*");

        id.IsWildcard.Should()
          .BeTrue();

        id.IsPrefix.Should()
          .BeTrue();

        id.IsSuffix.Should()
          .BeFalse();
    }

    [Fact]
    public void Suffix()
    {
        var id = new StreamId("*a");

        id.Value.Should()
          .Be("*a");

        id.IsWildcard.Should()
          .BeTrue();

        id.IsPrefix.Should()
          .BeFalse();

        id.IsSuffix.Should()
          .BeTrue();
    }

    [Fact]
    public void AllHasStar()
    {
        StreamId id = StreamId.All;
        id.Value.Should()
          .Be("*");
    }

    [Fact]
    public void PrefixEndsWithStar()
    {
        StreamId id = StreamId.Prefix("a");
        id.Value.Should()
          .Be("a*");
    }

    [Fact]
    public void SuffixStartsWithStar()
    {
        StreamId id = StreamId.Suffix("a");
        id.Value.Should()
          .Be("*a");
    }

    [Theory]
    [InlineData("**")]
    [InlineData("*a*")]
    [InlineData("*a*b*")]
    public void InvalidWildcardThrows(string value)
    {
        Assert.Throws<ArgumentException>(() => new StreamId(value));
    }

    [Fact]
    public void SameValueIsEqual()
    {
        StreamId id1 = "a";
        StreamId id2 = "a";

        id1.Equals(id2)
           .Should()
           .BeTrue();

        id1.Equals((object)id2)
           .Should()
           .BeTrue();

        (id1 == id2).Should()
                    .BeTrue();
    }

    [Fact]
    public void DifferentValueIsNotEqual()
    {
        StreamId id1 = "a";
        StreamId id2 = "b";

        id1.Equals(id2)
           .Should()
           .BeFalse();

        id1.Equals((object)id2)
           .Should()
           .BeFalse();

        (id1 != id2).Should()
                    .BeTrue();
    }
}