using System;
using FluentAssertions;
using Xunit;

namespace AppCoreNet.EventStore.Subscription;

public class SubscriptionIdTests
{
    [Fact]
    public void All()
    {
        var id = new SubscriptionId("*");

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
        var id = new SubscriptionId("a*");

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
        var id = new SubscriptionId("*a");

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
        SubscriptionId id = SubscriptionId.All;
        id.Value.Should()
          .Be("*");
    }

    [Fact]
    public void PrefixEndsWithStar()
    {
        SubscriptionId id = SubscriptionId.Prefix("a");
        id.Value.Should()
          .Be("a*");
    }

    [Fact]
    public void SuffixStartsWithStar()
    {
        SubscriptionId id = SubscriptionId.Suffix("a");
        id.Value.Should()
          .Be("*a");
    }

    [Theory]
    [InlineData("**")]
    [InlineData("*a*")]
    [InlineData("*a*b*")]
    public void InvalidWildcardThrows(string value)
    {
        Assert.Throws<ArgumentException>(() => new SubscriptionId(value));
    }

    [Fact]
    public void SameValueIsEqual()
    {
        SubscriptionId id1 = "a";
        SubscriptionId id2 = "a";

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
        SubscriptionId id1 = "a";
        SubscriptionId id2 = "b";

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