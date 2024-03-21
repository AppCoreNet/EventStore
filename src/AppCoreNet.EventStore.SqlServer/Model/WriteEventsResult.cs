// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class WriteEventsResult
{
    public int StatusCode { get; init; }

    public long? Sequence { get; set; }

    public long? Position { get; set; }
}