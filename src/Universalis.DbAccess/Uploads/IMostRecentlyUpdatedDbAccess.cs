using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IMostRecentlyUpdatedDbAccess
{
    public Task Create(MostRecentlyUpdated document, CancellationToken cancellationToken = default);

    public Task Push(uint worldId, WorldItemUpload document, CancellationToken cancellationToken = default);

    public Task<MostRecentlyUpdated> Retrieve(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default);

    public Task<IList<MostRecentlyUpdated>> RetrieveMany(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default);
}