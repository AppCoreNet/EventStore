namespace AppCoreNet.EventStore.SqlServer;

internal static class WatchEventsResultCode
{
    public const int Success = 0;
    public const int StreamNotFound = 1;
    public const int Deleted = 2;
}