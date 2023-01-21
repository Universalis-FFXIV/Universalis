using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class ListingStore : IListingStore
{
    private readonly ILogger<ListingStore> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public ListingStore(NpgsqlDataSource dataSource, ILogger<ListingStore> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task UpsertLive(IEnumerable<Listing> listings, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.UpsertLive");

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Get the currently timestamp for the batch
        var uploadedAt = DateTimeOffset.Now;

        // Listings are grouped for better exceptions if a batch fails; exceptions can be
        // filtered by world and item.
        var groupedListings = listings.GroupBy(l => new WorldItemPair(l.WorldId, l.ItemId));
        foreach (var listingGroup in groupedListings)
        {
            // Npgsql batches have an implicit transaction around them
            // https://www.npgsql.org/doc/basic-usage.html#batching
            await using var batch = new NpgsqlBatch(connection);

            foreach (var listing in listingGroup)
            {
                // If a listing is uploaded multiple times in separate uploads, it
                // can already be in the database, causing a conflict. To handle that,
                // we just update the existing record and ensure that it's made live
                // again. It's not clear to me what happens on the game servers when
                // a listing is updated. Until we have more data, I'm assuming that
                // all updates are the same as new listings.
                batch.BatchCommands.Add(new NpgsqlBatchCommand("INSERT INTO listing " +
                                                               "(listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, creator_id, creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, seller_id, uploaded_at, source) " +
                                                               "VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18) " +
                                                               "ON CONFLICT (listing_id) DO UPDATE SET last_review_time = EXCLUDED.last_review_time, uploaded_at = EXCLUDED.uploaded_at")
                {
                    Parameters =
                    {
                        new NpgsqlParameter<string> { TypedValue = listing.ListingId },
                        new NpgsqlParameter<int> { TypedValue = listing.ItemId },
                        new NpgsqlParameter<int> { TypedValue = listing.WorldId },
                        new NpgsqlParameter<bool> { TypedValue = listing.Hq },
                        new NpgsqlParameter<bool> { TypedValue = listing.OnMannequin },
                        new NpgsqlParameter
                            { Value = ConvertMateriaToJArray(listing.Materia), NpgsqlDbType = NpgsqlDbType.Jsonb },
                        new NpgsqlParameter<int> { TypedValue = listing.PricePerUnit },
                        new NpgsqlParameter<int> { TypedValue = listing.Quantity },
                        new NpgsqlParameter<int> { TypedValue = listing.DyeId },
                        new NpgsqlParameter<string> { TypedValue = listing.CreatorId },
                        new NpgsqlParameter<string> { TypedValue = listing.CreatorName },
                        new NpgsqlParameter<DateTime> { TypedValue = listing.LastReviewTime },
                        new NpgsqlParameter<string> { TypedValue = listing.RetainerId },
                        new NpgsqlParameter<string> { TypedValue = listing.RetainerName },
                        new NpgsqlParameter<int> { TypedValue = listing.RetainerCityId },
                        new NpgsqlParameter<string> { TypedValue = listing.SellerId },
                        new NpgsqlParameter<DateTime> { TypedValue = uploadedAt.UtcDateTime },
                        new NpgsqlParameter<string> { TypedValue = listing.Source },
                    },
                });
            }

            try
            {
                await batch.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to insert listings (world={}, item={})", listingGroup.Key.WorldId,
                    listingGroup.Key.ItemId);
                throw;
            }
        }
    }

    public async Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveLive");

        await using var command = _dataSource.CreateCommand(
            """
            WITH cte AS(
                SELECT MAX(uploaded_at) as max_uploaded_at FROM listing
                WHERE item_id = $1 AND world_id = $2
            )
            SELECT t.listing_id, t.item_id, t.world_id, t.hq, t.on_mannequin, t.materia,
                   t.unit_price, t.quantity, t.dye_id, t.creator_id, t.creator_name,
                   t.last_review_time, t.retainer_id, t.retainer_name, t.retainer_city_id,
                   t.seller_id, t.uploaded_at, t.source
            FROM public.listing t
            WHERE t.item_id = $1 AND t.world_id = $2 AND t.uploaded_at = (SELECT max_uploaded_at FROM cte)
            ORDER BY unit_price
            """);
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var listings = new List<Listing>();
            while (await reader.ReadAsync(cancellationToken))
            {
                listings.Add(new Listing
                {
                    ListingId = reader.GetString(0),
                    ItemId = reader.GetInt32(1),
                    WorldId = reader.GetInt32(2),
                    Hq = reader.GetBoolean(3),
                    OnMannequin = reader.GetBoolean(4),
                    Materia = ConvertMateriaFromJArray(reader.GetFieldValue<JArray>(5)),
                    PricePerUnit = reader.GetInt32(6),
                    Quantity = reader.GetInt32(7),
                    DyeId = reader.GetInt32(8),
                    CreatorId = reader.GetString(9),
                    CreatorName = reader.GetString(10),
                    LastReviewTime = reader.GetDateTime(11),
                    RetainerId = reader.GetString(12),
                    RetainerName = reader.GetString(13),
                    RetainerCityId = reader.GetInt32(14),
                    SellerId = reader.GetString(15),
                    UpdatedAt = reader.GetDateTime(16),
                    Source = reader.GetString(17),
                });
            }

            return listings;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (world={}, item={})", query.WorldId, query.ItemId);
            throw;
        }
    }

    public async Task<IDictionary<WorldItemPair, IList<Listing>>> RetrieveManyLive(ListingManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveManyLive");

        var worldIds = query.WorldIds.ToList();
        var itemIds = query.ItemIds.ToList();
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection);

        foreach (var (worldId, itemId) in worldItemPairs)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(
                """
                WITH cte AS(
                    SELECT MAX(uploaded_at) as max_uploaded_at FROM listing
                    WHERE item_id = $1 AND world_id = $2
                )
                SELECT t.listing_id, t.item_id, t.world_id, t.hq, t.on_mannequin, t.materia,
                       t.unit_price, t.quantity, t.dye_id, t.creator_id, t.creator_name,
                       t.last_review_time, t.retainer_id, t.retainer_name, t.retainer_city_id,
                       t.seller_id, t.uploaded_at, t.source
                FROM public.listing t
                WHERE t.item_id = $1 AND t.world_id = $2 AND t.uploaded_at = (SELECT max_uploaded_at FROM cte)
                ORDER BY unit_price
                """)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = itemId },
                    new NpgsqlParameter<int> { TypedValue = worldId },
                },
            });
        }

        try
        {
            await using var reader =
                await batch.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var listings = new Dictionary<WorldItemPair, IList<Listing>>();
            var batchesRead = 0;
            do
            {
                var key = worldItemPairs[batchesRead];
                var batchResult = new List<Listing>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    batchResult.Add(new Listing
                    {
                        ListingId = reader.GetString(0),
                        ItemId = reader.GetInt32(1),
                        WorldId = reader.GetInt32(2),
                        Hq = reader.GetBoolean(3),
                        OnMannequin = reader.GetBoolean(4),
                        Materia = ConvertMateriaFromJArray(reader.GetFieldValue<JArray>(5)),
                        PricePerUnit = reader.GetInt32(6),
                        Quantity = reader.GetInt32(7),
                        DyeId = reader.GetInt32(8),
                        CreatorId = reader.GetString(9),
                        CreatorName = reader.GetString(10),
                        LastReviewTime = reader.GetDateTime(11),
                        RetainerId = reader.GetString(12),
                        RetainerName = reader.GetString(13),
                        RetainerCityId = reader.GetInt32(14),
                        SellerId = reader.GetString(15),
                        UpdatedAt = reader.GetDateTime(16),
                        Source = reader.GetString(17),
                    });
                }

                listings[key] = batchResult;

                batchesRead++;
                await reader.NextResultAsync(cancellationToken);
            } while (batchesRead != worldItemPairs.Count);

            return listings;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (worlds={}, items={})", string.Join(',', worldIds),
                string.Join(',', itemIds));
            throw;
        }
    }

    private static JArray ConvertMateriaToJArray(IEnumerable<Materia> materia)
    {
        return materia
            .Select(m => new JObject { ["slot_id"] = m.SlotId, ["materia_id"] = m.MateriaId })
            .Aggregate(new JArray(), (array, o) =>
            {
                array.Add(o);
                return array;
            });
    }

    private static List<Materia> ConvertMateriaFromJArray(IEnumerable<JToken> materia)
    {
        return materia
            .Select(m => new Materia { SlotId = m["slot_id"].Value<int>(), MateriaId = m["materia_id"].Value<int>() })
            .ToList();
    }
}