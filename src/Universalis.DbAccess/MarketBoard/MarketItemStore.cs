using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Common.Caching;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly string _connectionString;
    private readonly ICache<MarketItemKey, MarketItem> _cache;

    public MarketItemStore(string connectionString)
    {
        _connectionString = connectionString;
        _cache = new MemoryCache<MarketItemKey, MarketItem>(100000);
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO market_item (world_id, item_id, updated) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.ItemId) },
                    new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime },
                },
            };

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.ConstraintName == "PK_market_item_item_id_world_id")
        {
            // Race condition; unique constraint violated
        }

        await _cache.Set(new MarketItemKey { WorldId = marketItem.WorldId, ItemId = marketItem.ItemId }, marketItem,
            cancellationToken);
    }

    public async Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        if (await Retrieve(marketItem.WorldId, marketItem.ItemId, cancellationToken) == null)
        {
            await Insert(marketItem, cancellationToken);
            return;
        }

        await using var command =
            new NpgsqlCommand(
                "UPDATE market_item SET updated = $1 WHERE world_id = $2 AND item_id = $3", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.ItemId) },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);

        await _cache.Set(new MarketItemKey { WorldId = marketItem.WorldId, ItemId = marketItem.ItemId }, marketItem,
            cancellationToken);
    }

    public async ValueTask<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        var key = new MarketItemKey { WorldId = worldId, ItemId = itemId };
        var cached = await _cache.Get(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(
                "SELECT world_id, item_id, updated FROM market_item WHERE world_id = $1 AND item_id = $2", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(worldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(itemId) },
                },
            };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        var newItem = new MarketItem
        {
            WorldId = Convert.ToUInt32(reader.GetInt32(0)),
            ItemId = Convert.ToUInt32(reader.GetInt32(1)),
            LastUploadTime = (DateTime)reader.GetValue(2),
        };

        await _cache.Set(key, newItem, cancellationToken);
        return newItem;
    }

    private class MarketItemKey : IEquatable<MarketItemKey>, ICopyable
    {
        public uint WorldId { get; init; }

        public uint ItemId { get; init; }

        public ICopyable Clone()
        {
            return (ICopyable)MemberwiseClone();
        }

        public bool Equals(MarketItemKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return WorldId == other.WorldId && ItemId == other.ItemId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((MarketItemKey)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(WorldId, ItemId);
        }

        public static bool operator ==(MarketItemKey left, MarketItemKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MarketItemKey left, MarketItemKey right)
        {
            return !Equals(left, right);
        }
    }
}