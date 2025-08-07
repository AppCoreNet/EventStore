// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppCoreNet.Diagnostics;
using AppCoreNet.EventStore.Serialization;
using EventStore.Client;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.EventStoreDb;

/// <summary>
/// Provides a <see cref="IEventStore"/> implementation using EventStore / Kurrent.
/// </summary>
public sealed class EventStoreDbEventStore : IEventStore
{
    private readonly EventStoreClient _client;
    private readonly IEventStoreSerializer _serializer;
    private readonly EventStoreDbOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDbEventStore"/> class.
    /// </summary>
    /// <param name="client">The client used to access the event store.</param>
    /// <param name="serializer">The serializer used to serialize/deserialize events.</param>
    /// <param name="optionsMonitor">The <see cref="IOptionsMonitor{TOptions}"/> used to resolve the <see cref="EventStoreDbOptions"/>.</param>
    public EventStoreDbEventStore(
        EventStoreClient client,
        IEventStoreSerializer serializer,
        IOptionsMonitor<EventStoreDbOptions> optionsMonitor)
    {
        Ensure.Arg.NotNull(client);
        Ensure.Arg.NotNull(serializer);
        Ensure.Arg.NotNull(optionsMonitor);

        _client = client;
        _serializer = serializer;
        _options = optionsMonitor.CurrentValue;
    }

    private string GetStreamName(StreamId streamId)
    {
        string streamName = streamId.ToString();
        return _options.StreamNamePrefix != null
            ? $"{_options.StreamNamePrefix}{streamName}"
            : streamName;
    }

    private async Task<IReadOnlyCollection<string>> GetStreamNamesAsync(StreamId streamId, CancellationToken cancellationToken = default)
    {
        var streamNames = new List<string>();

        try
        {
            EventStoreClient.ReadStreamResult result = _client.ReadStreamAsync(
                Direction.Forwards,
                "$streams",
                global::EventStore.Client.StreamPosition.Start,
                cancellationToken: cancellationToken);

            await foreach (ResolvedEvent resolvedEvent in result.ConfigureAwait(false))
            {
                if (resolvedEvent.Event.EventType == "$>")
                {
                    string eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
                    string[] streamPositionAndName = eventData.Split('@');
                    long streamPosition = long.Parse(streamPositionAndName[0], CultureInfo.InvariantCulture);
                    string streamName = streamPositionAndName[1];

                    // skip metadata streams
                    if (streamName.StartsWith("$$"))
                        continue;

                    bool streamMatches = false;
                    if (streamId.IsWildcard)
                    {
                        if (streamId.IsPrefix)
                        {
                            streamMatches = streamName.StartsWith(
                                streamId.Value.TrimEnd('*'),
                                StringComparison.Ordinal);
                        }
                        else if (streamId.IsSuffix)
                        {
                            streamMatches = streamName.EndsWith(
                                streamId.Value.TrimStart('*'),
                                StringComparison.Ordinal);
                        }
                        else
                        {
                            streamMatches = true;
                        }
                    }
                    else
                    {
                        streamMatches = streamName.Equals(
                            streamId.Value,
                            StringComparison.OrdinalIgnoreCase);
                    }

                    if (streamMatches)
                    {
                        streamNames.Add(streamName);
                    }
                }
            }
        }
        catch (global::EventStore.Client.StreamNotFoundException)
        {
            // we dont have any streams
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }

        return streamNames;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        StreamId streamId,
        IEnumerable<object> events,
        StreamState state,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotWildcard(streamId);
        Ensure.Arg.NotNull(events);

        string streamName = GetStreamName(streamId);

        IEnumerable<EventEnvelope> eventEnvelopes = events.Select(
            e => e as EventEnvelope ?? new EventEnvelope(e));

        EventData[] eventData =
            eventEnvelopes.Select(e => Utils.MapEventEnvelope(_serializer, e))
                          .ToArray();

        try
        {
            if (state == StreamState.Any || state == StreamState.None)
            {
                await _client.AppendToStreamAsync(
                                 streamName,
                                 Utils.MapStreamState(state),
                                 eventData,
                                 cancellationToken: cancellationToken)
                             .ConfigureAwait(false);
            }
            else
            {
                await _client.AppendToStreamAsync(
                                 streamName,
                                 new StreamRevision((ulong)state.Value),
                                 eventData,
                                 cancellationToken: cancellationToken)
                             .ConfigureAwait(false);
            }
        }
        catch (WrongExpectedVersionException)
        {
            throw new StreamStateException(streamName, state);
        }
        catch (global::EventStore.Client.StreamDeletedException)
        {
            throw new StreamDeletedException(streamName);
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<EventEnvelope>> ReadAsync(
        StreamId streamId,
        StreamPosition position,
        StreamReadDirection direction = StreamReadDirection.Forward,
        int maxCount = 1,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.InRange(maxCount, 0, int.MaxValue);

        string streamName = GetStreamName(streamId);

        if (direction == StreamReadDirection.Forward && position == StreamPosition.End)
        {
            direction = StreamReadDirection.Backward;
            maxCount = 1;
        }

        var result = new List<EventEnvelope>();

        try
        {
            if (streamId.IsWildcard)
            {
                EventStoreClient.ReadAllStreamResult events = _client.ReadAllAsync(
                    Utils.MapDirection(direction),
                    Utils.MapPosition(position),
                    Utils.MapStreamFilter(streamName),
                    maxCount,
                    cancellationToken: cancellationToken);

                await foreach (ResolvedEvent e in events.ConfigureAwait(false))
                {
                    if (e.Event.EventType.StartsWith("$"))
                        continue;

                    result.Add(Utils.MapEventRecord(_serializer, e.Event));
                }
            }
            else
            {
                EventStoreClient.ReadStreamResult events = _client.ReadStreamAsync(
                    Utils.MapDirection(direction),
                    streamName,
                    Utils.MapStreamPosition(position),
                    maxCount,
                    cancellationToken: cancellationToken);

                await foreach (ResolvedEvent e in events.ConfigureAwait(false))
                {
                    result.Add(Utils.MapEventRecord(_serializer, e.Event));
                }
            }
        }
        catch (global::EventStore.Client.StreamNotFoundException)
        {
            throw new StreamNotFoundException(streamName);
        }
        catch (global::EventStore.Client.StreamDeletedException)
        {
            throw new StreamDeletedException(streamName);
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<WatchEventResult?> WatchAsync(
        StreamId streamId,
        StreamPosition position,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);

        // TODO: watch prefix/suffix streams
        if (streamId.IsPrefix || streamId.IsSuffix)
            throw new NotImplementedException();

        if (position != StreamPosition.End)
        {
            IReadOnlyCollection<EventEnvelope> lastEvents = await ReadAsync(
                    streamId,
                    StreamPosition.End,
                    StreamReadDirection.Backward,
                    1,
                    cancellationToken)
                .ConfigureAwait(false);

            if (lastEvents.Count > 0)
            {
                EventEnvelope lastEvent = lastEvents.First();
                if (position.Value < lastEvent.Metadata.Index)
                {
                    return new WatchEventResult(
                        streamId.IsWildcard
                            ? lastEvent.Metadata.Sequence
                            : lastEvent.Metadata.Index);
                }
            }
        }

        string streamName = GetStreamName(streamId);

        var completed = new TaskCompletionSource<long>();
        using var timeoutTokenSource = new CancellationTokenSource(timeout);
        using CancellationTokenRegistration timeoutTokenRegistration =
            timeoutTokenSource.Token.Register(() => completed.TrySetCanceled(timeoutTokenSource.Token));

        Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventHandler
            = (_, e, _) =>
            {
                completed.TrySetResult(e.OriginalEventNumber.ToInt64());
                return Task.CompletedTask;
            };

        Action<StreamSubscription, SubscriptionDroppedReason, Exception?> droppedHandler
            = (_, reason, exception) =>
            {
                if (reason == SubscriptionDroppedReason.Disposed)
                {
                    // subscription was disposed, no need to set exception
                    return;
                }

                completed.TrySetException(
                    exception ?? new ApplicationException($"Subscription dropped: {reason.ToString()}"));
            };

        try
        {
            if (streamId.IsWildcard)
            {
                using (await _client.SubscribeToAllAsync(
                                        Utils.MapFromAll(position),
                                        eventHandler,
                                        subscriptionDropped: droppedHandler,
                                        cancellationToken: cancellationToken)
                                    .ConfigureAwait(false))
                {
                }
            }
            else
            {
                using (await _client.SubscribeToStreamAsync(
                                        streamName,
                                        Utils.MapFromStream(position),
                                        eventHandler,
                                        subscriptionDropped: droppedHandler,
                                        cancellationToken: cancellationToken)
                                    .ConfigureAwait(false))
                {
                }
            }
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }

        try
        {
            long result = await completed.Task.ConfigureAwait(false);
            return new WatchEventResult(result);
        }
        catch (TaskCanceledException e)
        {
            if (e.CancellationToken == timeoutTokenSource.Token)
                return null;

            throw;
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        StreamId streamId,
        CancellationToken cancellationToken = default)
    {
        Ensure.Arg.NotNull(streamId);
        Ensure.Arg.NotWildcard(streamId);

        string streamName = GetStreamName(streamId);
        try
        {
            await _client.TombstoneAsync(
                             streamName,
                             global::EventStore.Client.StreamState.StreamExists,
                             cancellationToken: cancellationToken)
                         .ConfigureAwait(false);
        }
        catch (WrongExpectedVersionException)
        {
            throw new StreamNotFoundException(streamName);
        }
        catch (global::EventStore.Client.StreamDeletedException)
        {
            throw new StreamDeletedException(streamName);
        }
        catch (Exception error)
        {
            throw ExceptionHelper.Rethrow(error);
        }
    }
}