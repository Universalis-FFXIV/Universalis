using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public interface IWorldUploadCountDbAccess
    {
        public Task Create(WorldUploadCount document);

        public Task<WorldUploadCount> Retrieve(WorldUploadCountQuery query);

        public Task Update(WorldUploadCount document, WorldUploadCountQuery query);

        public Task Increment(WorldUploadCountQuery query);

        public Task Delete(WorldUploadCountQuery query);
    }
}