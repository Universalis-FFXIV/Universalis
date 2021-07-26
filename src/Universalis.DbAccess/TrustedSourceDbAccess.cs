using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>
    {
        public TrustedSourceDbAccess() : base("universalis", "trustedSources") { }
    }
}