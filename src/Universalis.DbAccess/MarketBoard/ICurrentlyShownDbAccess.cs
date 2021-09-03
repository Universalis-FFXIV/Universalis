using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public interface ICurrentlyShownDbAccess
    {
        public Task Create(CurrentlyShown document, CancellationToken cancellationToken = default);

        public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default);

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default);

        public Task<IList<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count,
            UploadOrder order, CancellationToken cancellationToken = default);

        public Task Update(CurrentlyShown document, CurrentlyShownQuery query, CancellationToken cancellationToken = default);

        public Task Delete(CurrentlyShownQuery query, CancellationToken cancellationToken = default);
    }
}