using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess;

public interface IScoreboardStore<TKey>
{
    Task SetScore(string scoreboardName, TKey id, double val);
    
    Task<IList<KeyValuePair<TKey, double>>> GetAllScores(string scoreboardName, int stop = -1);
}