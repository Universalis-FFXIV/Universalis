using StackExchange.Redis;

namespace Universalis.DbAccess;

public class WrappedRedisMultiplexer : ICacheRedisMultiplexer, IPersistentRedisMultiplexer
{
    private readonly IConnectionMultiplexer[] _connectionMultiplexers;
    private int _next;
    
    public WrappedRedisMultiplexer(params IConnectionMultiplexer[] connectionMultiplexers)
    {
        _connectionMultiplexers = connectionMultiplexers;
        _next = 0;
    }

    IDatabase ICacheRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        // No, this isn't thread-safe.
        var mux = _connectionMultiplexers[_next];
        _next = (_next + 1) % _connectionMultiplexers.Length;
        return mux.GetDatabase(db, asyncObject);
    }

    IDatabase IPersistentRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        var mux = _connectionMultiplexers[_next];
        _next = (_next + 1) % _connectionMultiplexers.Length;
        return mux.GetDatabase(db, asyncObject);
    }

    public IConnectionMultiplexer[] GetConnectionMultiplexers() => _connectionMultiplexers;
}
