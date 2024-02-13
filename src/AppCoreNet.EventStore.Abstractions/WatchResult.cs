using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public sealed class WatchResult
{
    /// <summary>
    /// Gets the token which can be used in subsequent calls.
    /// </summary>
    public string ContinuationToken { get; }

    /// <summary>
    /// Gets the ID of the stream for which new events are available.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the current stream position.
    /// </summary>
    public long StreamPosition { get; }

    public WatchResult(string streamId, long streamPosition, string continuationToken)
    {
        Ensure.Arg.NotEmpty(continuationToken);
        Ensure.Arg.NotEmpty(streamId);
        Ensure.Arg.InRange(streamPosition, 0, long.MaxValue);

        StreamId = streamId;
        ContinuationToken = continuationToken;
        StreamPosition = streamPosition;
    }
}