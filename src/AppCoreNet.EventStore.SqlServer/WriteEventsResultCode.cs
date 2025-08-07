// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer;

internal static class WriteEventsResultCode
{
    public const int Success = 0;
    public const int InvalidStreamState = 1;
    public const int StreamDeleted = 2;
}