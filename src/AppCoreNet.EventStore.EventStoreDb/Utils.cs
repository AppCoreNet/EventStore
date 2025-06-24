using System;
using System.Collections.Generic;
using System.Text;
using AppCoreNet.EventStore.Serialization;
using EventStore.Client;

namespace AppCoreNet.EventStore.EventStoreDb;

internal static class Utils
{
    public static global::EventStore.Client.StreamState MapStreamState(StreamState state)
    {
        return state == StreamState.Any
            ? global::EventStore.Client.StreamState.Any
            : global::EventStore.Client.StreamState.NoStream;
    }

    public static Direction MapStreamReadDirection(StreamReadDirection direction)
    {
        return direction switch
        {
            StreamReadDirection.Forward => Direction.Forwards,
            StreamReadDirection.Backward => Direction.Backwards,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), $"Unsupported direction value: {direction}"),
        };
    }

    public static global::EventStore.Client.StreamPosition MapStreamPosition(StreamPosition position)
    {
        if (position == StreamPosition.Start)
            return global::EventStore.Client.StreamPosition.Start;

        if (position == StreamPosition.End)
            return global::EventStore.Client.StreamPosition.End;

        return global::EventStore.Client.StreamPosition.FromInt64(position.Value);
    }

    public static Position MapPosition(StreamPosition position)
    {
        if (position == StreamPosition.Start)
            return Position.Start;

        if (position == StreamPosition.End)
            return Position.End;

        return new Position((ulong)position.Value, (ulong)position.Value);
    }

    public static IEventFilter MapStreamFilter(string streamName)
    {
        if (streamName.Equals("*"))
            return StreamFilter.None;

        if (streamName.StartsWith("*"))
            return StreamFilter.RegularExpression($".*{streamName.TrimStart('*')}$");

        if (streamName.EndsWith("*"))
            return StreamFilter.Prefix(streamName.TrimEnd('*'));

        throw new InvalidOperationException();
    }

    public static EventData MapEventEnvelope(IEventStoreSerializer serializer, EventEnvelope envelope)
    {
        string? serializedData = serializer.Serialize(envelope.Data);
        byte[]? data = serializedData != null
            ? Encoding.UTF8.GetBytes(serializedData)
            : null;

        string? serializedMetadata = serializer.Serialize(envelope.Metadata.Data);
        byte[]? metadata = serializedMetadata != null
            ? Encoding.UTF8.GetBytes(serializedMetadata)
            : null;

        return new EventData(
            Uuid.NewUuid(),
            envelope.EventTypeName,
            data,
            metadata,
            contentType: serializer.ContentType);
    }

    public static EventEnvelope MapEventRecord(IEventStoreSerializer serializer, EventRecord @event)
    {
        object data = serializer.Deserialize(
            @event.EventType,
            Encoding.UTF8.GetString(@event.Data.ToArray())) !;

        string? metadataString = @event.Metadata.Length == 0
            ? null
            : Encoding.UTF8.GetString(@event.Metadata.ToArray());

        var metadata = (Dictionary<string, string>?)serializer.Deserialize(
            Constants.StringDictionaryTypeName,
            metadataString);

        return new EventEnvelope(
            @event.EventType,
            data,
            new EventMetadata
            {
                Index = @event.EventNumber.ToInt64(),
                Sequence = (long)@event.Position.CommitPosition,
                CreatedAt = @event.Created,
                Data = metadata,
            });
    }
}