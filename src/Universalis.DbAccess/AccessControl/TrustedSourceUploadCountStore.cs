using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.AccessControl;

public class TrustedSourceUploadCountStore : ISourceUploadCountStore
{
    private readonly IConnectionMultiplexer _redis;

    public TrustedSourceUploadCountStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    public Task IncrementCounter(string key, string counterName)
    {
        var db = _redis.GetDatabase();
        return db.HashIncrementAsync(key, counterName);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetCounterValues(string key)
    {
        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(key);
        return entries.Select(e => new KeyValuePair<string, long>(e.Name, (long)e.Value)).ToList();
    }
}