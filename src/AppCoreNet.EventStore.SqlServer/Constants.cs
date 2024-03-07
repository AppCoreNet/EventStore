namespace AppCoreNet.EventStore.SqlServer;

internal static class Constants
{
    public const int StreamIdMaxLength = 64;

    public const int SubscriptionIdMaxLength = 64;

    public const string StreamIdAll = "*";

    public const int EventTypeMaxLength = 128;

    public static readonly string StringDictionaryTypeName = "StringDictionary";
}