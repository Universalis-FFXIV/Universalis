using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard
{
    public class MockCurrentlyShownDbAccess : ICurrentlyShownDbAccess
    {
        private readonly List<CurrentlyShown> _collection = new();

        public Task Create(CurrentlyShown document)
        {
            _collection.Add(document);
            return Task.CompletedTask;
        }

        public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query)
        {
            return Task.FromResult(_collection
                .FirstOrDefault(d => d.WorldId == query.WorldId && d.ItemId == query.ItemId));
        }

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query)
        {
            return Task.FromResult(_collection
                .Where(d => d.ItemId == query.ItemId && query.WorldIds.Contains(d.WorldId)));
        }

        public Task<IEnumerable<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count, UploadOrder order)
        {
            var documents = _collection
                .Where(o => query.WorldIds.Contains(o.WorldId))
                .Select(o => new WorldItemUpload
                {
                    WorldId = o.WorldId,
                    ItemId = o.ItemId,
                    LastUploadTimeUnixMilliseconds = o.LastUploadTimeUnixMilliseconds,
                })
                .ToList();

            documents.Sort((a, b) => order switch
            {
                UploadOrder.MostRecent => (int)b.LastUploadTimeUnixMilliseconds - (int)a.LastUploadTimeUnixMilliseconds,
                UploadOrder.LeastRecent => (int)a.LastUploadTimeUnixMilliseconds - (int)b.LastUploadTimeUnixMilliseconds,
                _ => throw new ArgumentException(nameof(order)),
            });
            
            return Task.FromResult(documents.Take(count));
        }

        public async Task Update(CurrentlyShown document, CurrentlyShownQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public async Task Delete(CurrentlyShownQuery query)
        {
            var document = await Retrieve(query);
            _collection.Remove(document);
        }
    }
}