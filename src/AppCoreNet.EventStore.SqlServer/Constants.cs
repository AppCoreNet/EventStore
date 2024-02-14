using System.Collections.Generic;

namespace AppCoreNet.EventStore.SqlServer;

internal static class Constants
{
    public const string EventStoreSchemaAnnotation = "AppCoreNet:EventStoreSchema";

    public const int StreamIdMaxLength = 64;

    public const string StreamIdAll = "$all";

    public const int EventTypeMaxLength = 128;

    public const string InsertEventsProcedureName = "sp_InsertEvents";

    public const string WatchEventsProcedureName = "sp_WatchEvents";

    public const string EventTableTypeName = "EventTable";

    public static readonly string StringDictionaryTypeName = "StringDictionary";
}