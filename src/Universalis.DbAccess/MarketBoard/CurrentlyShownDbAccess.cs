using System;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Priority_Queue;
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

        private readonly IMostRecentlyUpdatedDbAccess _mostRecentlyUpdatedDb;

        public CurrentlyShownDbAccess(IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb, IMongoClient client) : base(
            client, Constants.DatabaseName, "recentData")
        {
            _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
        }

        public CurrentlyShownDbAccess(IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb, IMongoClient client,
            string databaseName) : base(client, databaseName, "recentData")
        {
            _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
        }

        public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(query.ToFilterDefinition()).ToListAsync(cancellationToken);
        }

        public async Task<IList<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count, int toSkip, UploadOrder order, CancellationToken cancellationToken = default)
        {
            if (order == UploadOrder.MostRecent)
            {
                return await GetMostRecentlyUpdated(query, count, cancellationToken);
            }

            // This is a *very long* query right now, so we hold a static cache of all the responses
            // To mitigate potential thread pool starvation caused by this being requested repeatedly.
            var callState = new UploadTimeQueryCallState { WorldIds = query.WorldIds, Count = count, Order = order };
            if (UploadTimeQueryCache.ContainsKey(callState) && DateTime.Now - UploadTimeQueryCache[callState].QueryTime < new TimeSpan(0, 10, 0))
            {
                return UploadTimeQueryCache[callState].Uploads;
            }

            var sortBuilder = Builders<CurrentlyShown>.Sort;
            var sortDefinition = sortBuilder.Ascending(o => o.LastUploadTimeUnixMilliseconds);

            var projectDefinition = Builders<CurrentlyShown>.Projection
                .Include(o => o.WorldId)
                .Include(o => o.ItemId)
                .Include(o => o.LastUploadTimeUnixMilliseconds);

            var uploads = await Collection
                .Find(query.ToFilterDefinition())
                .Project<WorldItemUpload>(projectDefinition)
                .Sort(sortDefinition)
                .Skip(toSkip)
                .Limit(count)
                .ToListAsync(cancellationToken);

            UploadTimeQueryCache[callState] = new UploadTimeQueryResult { QueryTime = DateTime.Now, Uploads = uploads };

            return uploads;
        }

        private async Task<IList<WorldItemUpload>> GetMostRecentlyUpdated(
            CurrentlyShownWorldIdsQuery query,
            int count,
            CancellationToken cancellationToken = default)
        {
            // Single world case
            if (query.WorldIds.Length == 1)
            {
                var oneWorldData = await _mostRecentlyUpdatedDb.Retrieve(new MostRecentlyUpdatedQuery
                    { WorldId = query.WorldIds[0] }, cancellationToken);
                return oneWorldData.Uploads.Take(count).ToList();
            }

            // Data center case
            var multiWorldData = await _mostRecentlyUpdatedDb.RetrieveMany(new MostRecentlyUpdatedManyQuery { WorldIds = query.WorldIds }, cancellationToken);
            multiWorldData = multiWorldData.Where(d => d.Uploads.Any()).ToList();

            var heap = new SimplePriorityQueue<MostRecentlyUpdated, double>(Comparer<double>.Create((a, b) => (int)(b - a)));
            foreach (var d in multiWorldData)
            {
                // Build a heap with the first (most recent) element of each document as the priority
                heap.Enqueue(d, d.Uploads[0].LastUploadTimeUnixMilliseconds);
            }

            var outData = new List<WorldItemUpload>();
            while (outData.Count < count)
            {
                if (heap.Count == 0 || !heap.First.Uploads.Any()) break;

                // Keep pulling off the first item of the top document
                outData.Add(heap.First.Uploads[0]);
                heap.First.Uploads.RemoveAt(0);

                if (!heap.First.Uploads.Any())
                {
                    // Remove the top document
                    heap.Dequeue();
                }
                else
                {
                    // Update the priority if the top document wasn't removed
                    heap.UpdatePriority(heap.First, heap.First.Uploads[0].LastUploadTimeUnixMilliseconds);
                }
            }

            return outData;
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