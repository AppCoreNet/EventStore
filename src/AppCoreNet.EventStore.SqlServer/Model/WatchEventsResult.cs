﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer.Model;

internal sealed class WatchEventsResult
{
    public long? Position { get; set; }
}