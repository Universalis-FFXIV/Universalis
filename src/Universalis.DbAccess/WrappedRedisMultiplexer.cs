using System.Linq;
using StackExchange.Redis;

namespace Universalis.DbAccess;

public class WrappedRedisMultiplexer : ICacheRedisMultiplexer, IPersistentRedisMultiplexer
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public int ReplicaCount { get; }
    
    public WrappedRedisMultiplexer(IConnectionMultiplexer connectionMultiplexer)
    {
        // Count port numbers, assume single-master configuration
        ReplicaCount = connectionMultiplexer.Configuration.Select(c => c.Equals(':')).Count() - 1;
        _connectionMultiplexer = connectionMultiplexer;
    }

    IDatabase ICacheRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return _connectionMultiplexer.GetDatabase();
    }

    IDatabase IPersistentRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return _connectionMultiplexer.GetDatabase();
    }

    public IConnectionMultiplexer GetConnectionMultiplexer()
    {
        return _connectionMultiplexer;
    }
}
