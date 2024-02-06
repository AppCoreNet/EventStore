using System;

namespace AppCoreNet.EventStore.SqlServer;

internal static class Scripts
{
    public static FormattableString AppendEvents(string streamId) =>
        $"""
        DECLARE @streamId AS INT;
        DECLARE @latestStreamVersion AS INT;

        SELECT
            @streamId = EventStream.Id, @latestVersion = EventStream.[Version]
        FROM
            EventStream WITH (UPDLOCK, ROWLOCK)
        WHERE
            EventStream.StreamId = {streamId};

        IF @
        """;
}