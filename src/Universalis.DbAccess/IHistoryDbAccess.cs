using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public interface IHistoryDbAccess
    {
        public Task Create(History document);

        public Task<History> Retrieve(HistoryQuery query);

        public Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query);

        public Task Update(History document, HistoryQuery query);

        public Task Delete(HistoryQuery query);
    }
}