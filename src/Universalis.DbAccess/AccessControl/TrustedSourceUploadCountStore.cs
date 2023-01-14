using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Universalis.DbAccess.AccessControl;

public class TrustedSourceUploadCountStore : ISourceUploadCountStore
{
    private readonly IPersistentRedisMultiplexer _redis;

    public TrustedSourceUploadCountStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
    }
    
    public Task IncrementCounter(string key, string counterName)
    {
        using var activity = Util.ActivitySource.StartActivity("TrustedSourceUploadCountStore.IncrementCounter");

        var db = _redis.GetDatabase();
        return db.HashIncrementAsync(key, counterName);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetCounterValues(string key)
    {
        using var activity = Util.ActivitySource.StartActivity("TrustedSourceUploadCountStore.GetCounterValues");

        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(key);
        return entries.Select(e => new KeyValuePair<string, long>(e.Name, (long)e.Value)).ToList();
    }
}