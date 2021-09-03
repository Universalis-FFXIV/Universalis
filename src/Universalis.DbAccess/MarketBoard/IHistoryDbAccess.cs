using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public interface IHistoryDbAccess
    {
        public Task Create(History document, CancellationToken cancellationToken = default);

        public Task<History> Retrieve(HistoryQuery query, CancellationToken cancellationToken = default);

        public Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query, CancellationToken cancellationToken = default);

        public Task Update(History document, HistoryQuery query, CancellationToken cancellationToken = default);

        public Task Delete(HistoryQuery query, CancellationToken cancellationToken = default);
    }
}