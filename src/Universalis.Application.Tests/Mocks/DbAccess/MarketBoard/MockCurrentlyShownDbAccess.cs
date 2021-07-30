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