// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class ReadEventsCommand : SqlCommand<IReadOnlyCollection<Model.Event>>
{
    public required StreamId StreamId { get; init; }

    public required StreamPosition Position { get; init; }

    public required StreamReadDirection Direction { get; init; }

    public required int MaxCount { get; init; }

    public ReadEventsCommand(DbContext dbContext)
        : base(dbContext)
    {
    }

    protected override async Task<IReadOnlyCollection<Model.Event>> ExecuteCoreAsync(
        CancellationToken cancellationToken)
    {
        IQueryable<Model.Event> events =
            DbContext.Set<Model.Event>()
                     .AsNoTracking();

        int maxCount = MaxCount;

        bool isWildCardStreamId = StreamId.IsWildcard;

        switch (Direction)
        {
            case StreamReadDirection.Forward:
            {
                if (Position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Sequence);
                }
                else if (Position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Sequence);
                    maxCount = 1;
                }
                else
                {
                    events = events.OrderBy(e => e.Sequence);
                    events = isWildCardStreamId
                        ? events.Where(e => e.Sequence >= Position.Value)
                        : events.Where(e => e.Index >= Position.Value);
                }

                break;
            }

            case StreamReadDirection.Backward:
            {
                if (Position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Sequence);
                    maxCount = 1;
                }
                else if (Position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Sequence);
                }
                else
                {
                    events = events.OrderByDescending(e => e.Sequence);
                    events = isWildCardStreamId
                        ? events.Where(e => e.Sequence <= Position.Value)
                        : events.Where(e => e.Index <= Position.Value);
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null);
        }

        IQueryable<Model.EventStream> dbSet =
            DbContext.Set<Model.EventStream>()
                     .AsNoTracking();

        if (StreamId.IsWildcard)
        {
            if (StreamId.IsPrefix)
            {
                string streamIdPrefix = StreamId.Value.TrimEnd('*');
                dbSet = dbSet.Where(s => s.StreamId.StartsWith(streamIdPrefix));
            }
            else if (StreamId.IsSuffix)
            {
                string streamIdSuffix = StreamId.Value.TrimStart('*');
                dbSet = dbSet.Where(s => s.StreamId.EndsWith(streamIdSuffix));
            }
        }
        else
        {
            dbSet = dbSet.Where(s => s.StreamId == StreamId.Value);
        }

        if (StreamId.IsWildcard)
        {
            List<Model.Event> result =
                await events.Join(
                                dbSet,
                                e => e.EventStreamId,
                                s => s.Id,
                                (e, _) => e)
                            .Take(maxCount)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);

            return result;
        }

        var streamEvents =
            await dbSet.Select(
                           s =>
                               new
                               {
                                   Events = events.Where(e => e.EventStreamId == s.Id)
                                                  .Take(maxCount) // do not move Take() before Where()
                                                  .ToList(),
                               })
                       .ToListAsync(cancellationToken)
                       .ConfigureAwait(false);

        if (streamEvents.Count == 0)
        {
            throw new StreamNotFoundException(StreamId.Value);
        }

        return streamEvents.First()
                           .Events;
    }
}