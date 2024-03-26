// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Runtime.CompilerServices;
using AppCoreNet.EventStore;
using AppCoreNet.EventStore.Subscriptions;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Diagnostics;

internal static partial class Ensure
{
    internal static partial class Arg
    {
        public static void NotWildcard(
            StreamId? streamId,
            [CallerArgumentExpression("streamId")] string? parameterName = null)
        {
            if (streamId is not null && streamId.IsWildcard)
            {
                throw new ArgumentException("Stream ID must not contain wildcard characters.", parameterName);
            }
        }

        public static void NotWildcard(
            SubscriptionId? subscriptionId,
            [CallerArgumentExpression("subscriptionId")] string? parameterName = null)
        {
            if (subscriptionId is not null && subscriptionId.IsWildcard)
            {
                throw new ArgumentException(
                    "Subscription ID must not contain wildcard characters.",
                    parameterName);
            }
        }
    }
}