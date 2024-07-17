using System;
using System.Linq;
using StackExchange.Redis;

namespace Universalis.DbAccess;

public class WrappedRedisMultiplexer : ICacheRedisMultiplexer, IPersistentRedisMultiplexer
{
    private readonly IConnectionMultiplexer[] _connectionMultiplexers;

    public int ReplicaCount { get; }
    
    public WrappedRedisMultiplexer(params IConnectionMultiplexer[] connectionMultiplexers)
    {
        // Count port numbers, assume single-master configuration
        ReplicaCount = connectionMultiplexers[0].Configuration.Select(c => c.Equals(':')).Count() - 1;
        _connectionMultiplexers = connectionMultiplexers;
    }

    IDatabase ICacheRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return GetConnectionMultiplexer().GetDatabase();
    }

    IDatabase IPersistentRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return GetConnectionMultiplexer().GetDatabase();
    }

    public IConnectionMultiplexer GetConnectionMultiplexer()
    {
        // Uniformly load balance across multiplexers
        var index = Random.Shared.NextInt64(0, _connectionMultiplexers.Length);
        return _connectionMultiplexers[index];
    }
}
