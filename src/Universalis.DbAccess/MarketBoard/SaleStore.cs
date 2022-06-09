using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStore : ISaleStore
{
    private readonly string _connectionString;

    public SaleStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Insert(Sale sale, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO sale (id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = Guid.NewGuid() },
                    new NpgsqlParameter { Value = Convert.ToInt32(sale.WorldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(sale.ItemId) },
                    new NpgsqlParameter { Value = sale.Hq },
                    new NpgsqlParameter { Value = Convert.ToInt64(sale.PricePerUnit) },
                    new NpgsqlParameter { Value = Convert.ToInt32(sale.Quantity) },
                    new NpgsqlParameter { Value = sale.BuyerName },
                    new NpgsqlParameter { Value = sale.SaleTime },
                    new NpgsqlParameter { Value = sale.UploaderIdHash },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sale>> RetrieveBySaleTime(uint worldId, uint itemId, int count, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id FROM sale WHERE world_id = $1 AND item_id = $2 ORDER BY sale_time LIMIT $3", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = Convert.ToInt32(worldId) },
                    new NpgsqlParameter { Value = Convert.ToInt32(itemId) },
                    new NpgsqlParameter { Value = count },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var sales = new List<Sale>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sales.Add(new Sale
            {
                Id = reader.GetGuid(0),
                WorldId = Convert.ToUInt32(reader.GetInt32(1)),
                ItemId = Convert.ToUInt32(reader.GetInt32(2)),
                Hq = reader.GetBoolean(3),
                PricePerUnit = Convert.ToUInt32(reader.GetInt64(4)),
                Quantity = Convert.ToUInt32(reader.GetInt32(5)),
                BuyerName = reader.GetString(6),
                SaleTime = (DateTimeOffset)reader.GetValue(7),
                UploaderIdHash = reader.GetString(8),
            });
        }

        return sales;
    }
}