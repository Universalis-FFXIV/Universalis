using StackExchange.Redis;

namespace Universalis.DbAccess;

public interface ICacheRedisMultiplexer
{
    IDatabase GetDatabase(int db = -1, object asyncObject = null);
}
