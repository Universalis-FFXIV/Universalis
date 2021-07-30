using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard
{
    public class MockHistoryDbAccess : IHistoryDbAccess
    {
        private readonly List<History> _collection = new();

        public Task Create(History document)
        {
            _collection.Add(document);
            return Task.CompletedTask;
        }

        public Task<History> Retrieve(HistoryQuery query)
        {
            return Task.FromResult(_collection
                .FirstOrDefault(d => d.WorldId == query.WorldId && d.ItemId == query.ItemId));
        }

        public Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query)
        {
            return Task.FromResult(_collection
                .Where(d => d.ItemId == query.ItemId && query.WorldIds.Contains(d.WorldId)));
        }

        public async Task Update(History document, HistoryQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public async Task Delete(HistoryQuery query)
        {
            var document = await Retrieve(query);
            _collection.Remove(document);
        }
    }
}