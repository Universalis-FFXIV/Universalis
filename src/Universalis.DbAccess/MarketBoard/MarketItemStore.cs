using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly ILogger<MarketItemStore> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public MarketItemStore(NpgsqlDataSource dataSource, ILogger<MarketItemStore> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task SetData(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.Insert");

        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var command =
            _dataSource.CreateCommand(
                "INSERT INTO market_item (item_id, world_id, updated) VALUES ($1, $2, $3) ON CONFLICT (item_id, world_id) DO UPDATE SET updated = $3");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = marketItem.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = marketItem.WorldId });
        command.Parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={}, item={})", marketItem.WorldId,
                marketItem.ItemId);
            throw;
        }
    }

    public async ValueTask<MarketItem> GetData(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.Retrieve");

        await using var command =
            _dataSource.CreateCommand("SELECT updated FROM market_item VALUES WHERE item_id = $1 AND world_id = $2");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = itemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = worldId });

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new MarketItem
            {
                ItemId = itemId,
                WorldId = worldId,
                LastUploadTime = reader.GetDateTime(0),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve market item (world={}, item={})", worldId, itemId);
            throw;
        }
    }
}