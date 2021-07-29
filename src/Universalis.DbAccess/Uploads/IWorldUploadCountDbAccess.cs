using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IWorldUploadCountDbAccess
    {
        public Task Create(WorldUploadCount document);

        public Task<WorldUploadCount> Retrieve(WorldUploadCountQuery query);

        public Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts();

        public Task Update(WorldUploadCount document, WorldUploadCountQuery query);

        public Task Increment(WorldUploadCountQuery query);

        public Task Delete(WorldUploadCountQuery query);
    }
}