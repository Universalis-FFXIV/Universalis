using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IMostRecentlyUpdatedDbAccess
{
    public Task Push(int worldId, WorldItemUpload document, CancellationToken cancellationToken = default);

    public Task<IList<WorldItemUpload>> GetMostRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default);

    public Task<IList<WorldItemUpload>> GetAllMostRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default);
    
    public Task<IList<WorldItemUpload>> GetLeastRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default);

    public Task<IList<WorldItemUpload>> GetAllLeastRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default);
}