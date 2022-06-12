using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly string _connectionString;

    public MarketItemStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO market_item (world_id, item_id, updated) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(marketItem.ItemId) },
                    new NpgsqlParameter { Value = marketItem.LastUploadTime },
                },
            };

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.ErrorCode == -0x4005)
        {
            // Race condition; unique constraint violated
        }
    }
    
    public async Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
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
                    new NpgsqlParameter { Value = marketItem.LastUploadTime },
                    new NpgsqlParameter { Value = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(marketItem.ItemId) },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT world_id, item_id, updated FROM market_item WHERE world_id = $1 AND item_id = $2", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = Convert.ToInt32(worldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(itemId) },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        return new MarketItem
        {
            WorldId = Convert.ToUInt32(reader.GetInt32(0)),
            ItemId = Convert.ToUInt32(reader.GetInt32(1)),
            LastUploadTime = (DateTime)reader.GetValue(2),
        };
    }
}