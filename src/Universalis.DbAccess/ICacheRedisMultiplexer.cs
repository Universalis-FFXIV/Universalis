using StackExchange.Redis;

namespace Universalis.DbAccess;

public interface ICacheRedisMultiplexer
{
    int ReplicaCount { get; }

    IDatabase GetDatabase(int db = -1, object asyncObject = null);
}