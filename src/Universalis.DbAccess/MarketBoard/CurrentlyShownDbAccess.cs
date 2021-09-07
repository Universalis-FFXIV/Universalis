using System;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.MarketBoard
{
    public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>, ICurrentlyShownDbAccess
    {
        private static readonly ConcurrentDictionary<UploadTimeQueryCallState, UploadTimeQueryResult> UploadTimeQueryCache = new();

        public CurrentlyShownDbAccess(IMongoClient client) : base(
            client, Constants.DatabaseName, "recentData")
        {
        }

        public CurrentlyShownDbAccess(IMongoClient client,
            string databaseName) : base(client, databaseName, "recentData")
        {
        }

        public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(query.ToFilterDefinition()).ToListAsync(cancellationToken);
        }

        public async Task<IList<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count, UploadOrder order, CancellationToken cancellationToken = default)
        {
            // This is a *very long* query right now, so we hold a static cache of all the responses
            // To mitigate potential thread pool starvation caused by this being requested repeatedly.
            var callState = new UploadTimeQueryCallState { WorldIds = query.WorldIds, Count = count, Order = order };
            if (UploadTimeQueryCache.ContainsKey(callState) && DateTime.Now - UploadTimeQueryCache[callState].QueryTime < new TimeSpan(0, 10, 0))
            {
                return UploadTimeQueryCache[callState].Uploads;
            }

            var sortBuilder = Builders<CurrentlyShown>.Sort;
            var sortDefinition = order switch
            {
                UploadOrder.MostRecent => sortBuilder.Descending(o => o.LastUploadTimeUnixMilliseconds),
                UploadOrder.LeastRecent => sortBuilder.Ascending(o => o.LastUploadTimeUnixMilliseconds),
                _ => throw new NotSupportedException("Sort direction is invalid."),
            };

            var projectDefinition = Builders<CurrentlyShown>.Projection
                .Include(o => o.WorldId)
                .Include(o => o.ItemId)
                .Include(o => o.LastUploadTimeUnixMilliseconds);

            var uploads = await Collection
                .Find(query.ToFilterDefinition())
                .Project<WorldItemUpload>(projectDefinition)
                .Sort(sortDefinition)
                .Limit(count)
                .ToListAsync(cancellationToken);

            UploadTimeQueryCache[callState] = new UploadTimeQueryResult { QueryTime = DateTime.Now, Uploads = uploads };

            return uploads;
        }

        private static async Task<IList<WorldItemUpload>> GetMostRecentlyUpdated(
            IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb,
            CurrentlyShownWorldIdsQuery query,
            int count)
        {
            var data = await mostRecentlyUpdatedDb.RetrieveMany(new MostRecentlyUpdatedManyQuery { WorldIds = query.WorldIds });
            var agg = await data.ToAsyncEnumerable()
                .SelectMany(o => o.Uploads.ToAsyncEnumerable())
                .ToListAsync();
            agg.Sort((a, b) => (int)b.LastUploadTimeUnixMilliseconds - (int)a.LastUploadTimeUnixMilliseconds);
            return agg.Take(count).ToList();
        }

        private class UploadTimeQueryCallState : IEquatable<UploadTimeQueryCallState>
        {
            public uint[] WorldIds { get; init; }

            public int Count { get; init; }

            public UploadOrder Order { get; init; }

            public bool Equals(UploadTimeQueryCallState other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(WorldIds, other.WorldIds) && Count == other.Count && Order == other.Order;
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((UploadTimeQueryCallState)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(WorldIds, Count, (int)Order);
            }
        }

        private class UploadTimeQueryResult
        {
            public DateTime QueryTime { get; set; }

            public IList<WorldItemUpload> Uploads { get; set; }
        }
    }
}