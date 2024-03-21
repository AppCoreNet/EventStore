// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

namespace AppCoreNet.EventStore.SqlServer;

internal static class SchemaUtils
{
    public static string GetEventStoreSchema(string? schema)
    {
        return schema ?? "dbo";
    }
}