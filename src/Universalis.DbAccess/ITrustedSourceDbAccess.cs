using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public interface ITrustedSourceDbAccess
    {
        public Task Create(TrustedSource document);

        public Task<TrustedSource> Retrieve(TrustedSourceQuery query);

        public Task Update(TrustedSource document, TrustedSourceQuery query);

        public Task Delete(TrustedSourceQuery query);
    }
}