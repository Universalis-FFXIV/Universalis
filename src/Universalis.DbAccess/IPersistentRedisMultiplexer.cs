using StackExchange.Redis;

namespace Universalis.DbAccess;

public interface IPersistentRedisMultiplexer
{
    IDatabase GetDatabase(int db = -1, object asyncObject = null);
}
