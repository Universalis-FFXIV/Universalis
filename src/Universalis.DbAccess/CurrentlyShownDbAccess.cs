using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>
    {
        public CurrentlyShownDbAccess() : base("universalis", "recentData") { }
    }
}