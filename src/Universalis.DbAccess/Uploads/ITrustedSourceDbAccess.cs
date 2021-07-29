using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface ITrustedSourceDbAccess
    {
        public Task Create(TrustedSource document);

        public Task<TrustedSource> Retrieve(TrustedSourceQuery query);

        public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts();

        public Task Update(TrustedSource document, TrustedSourceQuery query);

        public Task Increment(TrustedSourceQuery query);

        public Task Delete(TrustedSourceQuery query);
    }
}