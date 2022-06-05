using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownDbAccess
{
    public Task<CurrentlyShownSimple> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default);

    public Task Update(CurrentlyShownSimple document, CurrentlyShownQuery query, CancellationToken cancellationToken = default);
}