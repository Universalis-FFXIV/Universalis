using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public class HistoryDbAccess : DbAccessService<History, HistoryQuery>
    {
        public HistoryDbAccess() : base("universalis", "extendedHistory") { }
    }
}