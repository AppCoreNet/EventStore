// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer;

internal static class DeleteStreamResultCode
{
    public const int Success = 0;
    public const int StreamNotFound = 1;
    public const int StreamAlreadyDeleted = 2;
}