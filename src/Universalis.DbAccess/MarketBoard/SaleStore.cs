using System;
using System.Collections.Generic;
using System.Linq;
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
        if (sale == null)
        {
            throw new ArgumentNullException(nameof(sale));
        }
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO sale (id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)", conn)
        {
            Parameters =
            {
                new NpgsqlParameter<Guid> { TypedValue = Guid.NewGuid() },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.WorldId) },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.ItemId) },
                new NpgsqlParameter<bool> { TypedValue = sale.Hq },
                new NpgsqlParameter<long> { TypedValue = Convert.ToInt64(sale.PricePerUnit) },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.Quantity) },
                new NpgsqlParameter<string> { TypedValue = sale.BuyerName },
                new NpgsqlParameter<DateTime> { TypedValue = sale.SaleTime },
                new NpgsqlParameter<string> { TypedValue = sale.UploaderIdHash },
                new NpgsqlParameter<bool> { TypedValue = sale.OnMannequin ?? false },
            },
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertMany(IEnumerable<Sale> sales, CancellationToken cancellationToken = default)
    {
        if (sales == null)
        {
            throw new ArgumentNullException(nameof(sales));
        }
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(conn);
        foreach (var sale in sales)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(
                "INSERT INTO sale (id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)")
            {
                Parameters =
                {
                    new NpgsqlParameter<Guid> { TypedValue = Guid.NewGuid() },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.ItemId) },
                    new NpgsqlParameter<bool> { TypedValue = sale.Hq },
                    new NpgsqlParameter<long> { TypedValue = Convert.ToInt64(sale.PricePerUnit) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.Quantity) },
                    new NpgsqlParameter<string> { TypedValue = sale.BuyerName },
                    new NpgsqlParameter<DateTime> { TypedValue = sale.SaleTime },
                    new NpgsqlParameter<string> { TypedValue = sale.UploaderIdHash },
                    new NpgsqlParameter<bool> { TypedValue = sale.OnMannequin ?? false },
                },
            });
        }
        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sale>> RetrieveBySaleTime(uint worldId, uint itemId, int count, DateTime? from = null, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin FROM sale WHERE world_id = $1 AND item_id = $2 AND sale_time <= $3 ORDER BY sale_time DESC LIMIT $4", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(worldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(itemId) },
                    new NpgsqlParameter<DateTime> { TypedValue = from ?? DateTime.UtcNow },
                    new NpgsqlParameter<int> { TypedValue = count + 20 }, // Give some buffer in case we filter out anything 
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var sales = new List<Sale>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var nextSale = new Sale
            {
                Id = reader.GetGuid(0),
                WorldId = Convert.ToUInt32(reader.GetInt32(1)),
                ItemId = Convert.ToUInt32(reader.GetInt32(2)),
                Hq = reader.GetBoolean(3),
                PricePerUnit = Convert.ToUInt32(reader.GetInt64(4)),
                Quantity = reader.IsDBNull(5) ? null : Convert.ToUInt32(reader.GetInt32(5)),
                BuyerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                SaleTime = (DateTime)reader.GetValue(7),
                UploaderIdHash = reader.IsDBNull(8) ? null : reader.GetString(8),
                OnMannequin = reader.IsDBNull(9) ? null : reader.GetBoolean(9),
            };

            if (sales.Contains(nextSale))
            {
                continue;
            }

            sales.Add(nextSale);
        }

        return sales.Take(count).ToList();
    }
}