using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class ReadEventsCommand : SqlCommand<IReadOnlyCollection<Model.Event>>
{
    required public StreamId StreamId { get; init; }

    required public StreamPosition Position { get; init; }

    required public StreamReadDirection Direction { get; init; }

    required public int MaxCount { get; init; }

    public ReadEventsCommand(DbContext dbContext)
        : base(dbContext)
    {
    }

    protected override async Task<IReadOnlyCollection<Model.Event>> ExecuteCoreAsync(CancellationToken cancellationToken)
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
                    events = events.OrderBy(e => e.Position);
                }
                else if (Position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Position);
                    maxCount = 1;
                }
                else
                {
                    events = events.OrderBy(e => e.Position);
                    events = isWildCardStreamId
                        ? events.Where(e => e.Sequence >= Position.Value)
                        : events.Where(e => e.Position >= Position.Value);
                }

                break;
            }

            case StreamReadDirection.Backward:
            {
                if (Position == StreamPosition.Start)
                {
                    events = events.OrderBy(e => e.Position);
                    maxCount = 1;
                }
                else if (Position == StreamPosition.End)
                {
                    events = events.OrderByDescending(e => e.Position);
                }
                else
                {
                    events = events.OrderByDescending(e => e.Position);
                    events = isWildCardStreamId
                        ? events.Where(e => e.Sequence <= Position.Value)
                        : events.Where(e => e.Position <= Position.Value);
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null);
        }

        try
        {
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

            var streamEvents =
                await dbSet.Select(
                               s =>
                                   new
                                   {
                                       Events = events.Where(e => e.EventStreamId == s.Id)
                                                      .Take(maxCount) // do not move Take() before Where()
                                                      .ToList(),
                                   })
                           .FirstOrDefaultAsync(cancellationToken)
                           .ConfigureAwait(false);

            if (streamEvents == null)
            {
                throw new EventStreamNotFoundException(StreamId.Value);
            }

            return streamEvents.Events;
        }
        catch (SqlException error)
        {
            throw new EventStoreException($"An error occured accessing the event store: {error.Message}", error);
        }
    }
}