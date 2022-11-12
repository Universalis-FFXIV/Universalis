using StackExchange.Redis;

namespace Universalis.DbAccess;

internal class WrappedRedisMultiplexer : ICacheRedisMultiplexer, IPersistentRedisMultiplexer
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    
    public WrappedRedisMultiplexer(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    IDatabase ICacheRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return _connectionMultiplexer.GetDatabase(db, asyncObject);
    }

    IDatabase IPersistentRedisMultiplexer.GetDatabase(int db, object asyncObject)
    {
        return _connectionMultiplexer.GetDatabase(db, asyncObject);
    }
}
