using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>, ITrustedSourceDbAccess
    {
        public TrustedSourceDbAccess() : base("universalis", "trustedSources") { }
    }
}