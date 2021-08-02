using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IUploadCountHistoryDbAccess
    {
        public Task Create(UploadCountHistory document);

        public Task<UploadCountHistory> Retrieve(UploadCountHistoryQuery query);
        
        public Task Update(uint lastPush, List<uint> uploadCountByDay);
    }
}