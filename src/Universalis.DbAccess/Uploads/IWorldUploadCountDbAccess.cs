using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IWorldUploadCountDbAccess
    {
        public Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts();
        
        public Task Increment(WorldUploadCountQuery query);
    }
}