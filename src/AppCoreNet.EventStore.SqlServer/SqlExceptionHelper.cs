// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace AppCoreNet.EventStore.SqlServer;

internal static class SqlExceptionHelper
{
    [StackTraceHidden]
    public static Exception Rethrow(SqlException error, CancellationToken cancellationToken = default)
    {
        // see https://github.com/dotnet/SqlClient/issues/26
        if (cancellationToken.IsCancellationRequested && error is { Number: 0, State: 0, Class: 11 })
            return new OperationCanceledException(null, error);

        return new EventStoreException($"An error occured accessing the event store: {error.Message}", error);
    }
}