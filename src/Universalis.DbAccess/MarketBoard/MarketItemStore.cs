using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public class MarketItemStore : IMarketItemStore
{
    private readonly ILogger<MarketItemStore> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public MarketItemStore(NpgsqlDataSource dataSource, ILogger<MarketItemStore> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
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

    public async ValueTask<MarketItem> Retrieve(MarketItemQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.Retrieve");

        await using var command =
            _dataSource.CreateCommand("SELECT updated FROM market_item WHERE item_id = $1 AND world_id = $2");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });

        try
        {
            var lastUploadTime = (DateTime?)await command.ExecuteScalarAsync(cancellationToken);
            if (!lastUploadTime.HasValue)
            {
                return null;
            }

            var marketItem = new MarketItem
            {
                ItemId = query.ItemId,
                WorldId = query.WorldId,
                LastUploadTime = lastUploadTime.Value,
            };

            return marketItem;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve market item (world={}, item={})", query.WorldId, query.ItemId);
            throw;
        }
    }

    public async ValueTask<IEnumerable<MarketItem>> RetrieveMany(MarketItemManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.RetrieveMany");

        var worldIds = query.WorldIds.ToList();
        var itemIds = query.ItemIds.ToList();
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        await using var command = _dataSource.CreateCommand(
            """
            SELECT updated, item_id, world_id
            FROM market_item
            WHERE item_id = ANY($1) AND world_id = ANY($2)
            """);
        command.Parameters.Add(new NpgsqlParameter<int[]>
            { TypedValue = worldItemPairs.Select(wip => wip.ItemId).Distinct().ToArray() });
        command.Parameters.Add(new NpgsqlParameter<int[]>
            { TypedValue = worldItemPairs.Select(wip => wip.WorldId).Distinct().ToArray() });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var marketItemRecords = new List<MarketItem>();
            while (await reader.ReadAsync(cancellationToken))
            {
                marketItemRecords.Add(new MarketItem
                {
                    LastUploadTime = reader.GetDateTime(0),
                    ItemId = reader.GetInt32(1),
                    WorldId = reader.GetInt32(2),
                });
            }

            return marketItemRecords;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (worlds={}, items={})", string.Join(',', worldIds),
                string.Join(',', itemIds));
            throw;
        }
    }
}