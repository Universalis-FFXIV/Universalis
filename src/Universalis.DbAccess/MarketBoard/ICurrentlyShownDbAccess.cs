using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public interface ICurrentlyShownDbAccess
    {
        public Task Create(CurrentlyShown document);

        public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query);

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query);

        public Task Update(CurrentlyShown document, CurrentlyShownQuery query);

        public Task Delete(CurrentlyShownQuery query);
    }
}