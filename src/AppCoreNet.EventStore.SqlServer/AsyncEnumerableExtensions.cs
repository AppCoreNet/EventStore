// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppCoreNet.EventStore.SqlServer;

internal static class AsyncEnumerableExtensions
{
    public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
    {
        await foreach (T item in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            return item;
        }

        throw new InvalidOperationException("The result did not contain the expected item.");
    }
}