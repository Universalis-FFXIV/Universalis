using StackExchange.Redis;
using System.Threading;

namespace Universalis.DbAccess;

internal class WrappedRedisMultiplexer : ICacheRedisMultiplexer, IPersistentRedisMultiplexer
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
        var muxIdx = Interlocked.Exchange(ref _next, _next++ % _connectionMultiplexers.Length);
        var mux = _connectionMultiplexers[muxIdx];
        return mux.GetDatabase(db, asyncObject);
    }

    IDatabase IPersistentRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        var muxIdx = Interlocked.Exchange(ref _next, _next++ % _connectionMultiplexers.Length);
        var mux = _connectionMultiplexers[muxIdx];
        return mux.GetDatabase(db, asyncObject);
    }
}
