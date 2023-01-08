using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class ListingStore : IListingStore
{
    private readonly NpgsqlDataSource _dataSource;

    public ListingStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task UpsertLive(IEnumerable<Listing> listingGroup, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var listingIds = new Dictionary<(int, int), List<string>>();
        foreach (var listing in listingGroup)
        {
            await using var command = new NpgsqlCommand(
                "INSERT INTO listing (listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, creator_id, creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, seller_id, live) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17) RETURNING listing_id",
                connection, transaction)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = listing.ListingId },
                    new NpgsqlParameter<int> { TypedValue = listing.ItemId },
                    new NpgsqlParameter<int> { TypedValue = listing.WorldId },
                    new NpgsqlParameter<bool> { TypedValue = listing.Hq },
                    new NpgsqlParameter<bool> { TypedValue = listing.OnMannequin },
                    new NpgsqlParameter { Value = ConvertMateriaToJArray(listing.Materia), NpgsqlDbType = NpgsqlDbType.Jsonb },
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
                    new NpgsqlParameter<bool> { TypedValue = true }, // listing.Live
                },
            };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            var key = (listing.ItemId, listing.WorldId);
            if (!listingIds.ContainsKey(key))
            {
                listingIds[key] = new List<string>();
            }
            
            listingIds[key].Add(reader.GetString(0));
        }

        foreach (var (itemId, worldId) in listingIds.Keys)
        {
            var ids = listingIds[(itemId, worldId)];
            await using var killOld = new NpgsqlCommand("UPDATE listing SET live = FALSE WHERE item_id = $1 AND world_id = $2 AND listing_id != ANY($3)", connection, transaction)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = itemId },
                    new NpgsqlParameter<int> { TypedValue = worldId },
                    new NpgsqlParameter<string[]> { TypedValue = ids.ToArray() },
                },
            };

            await killOld.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, creator_id, creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, seller_id FROM listing WHERE item_id = $1 AND world_id = $2 AND live ORDER BY unit_price");
        command.Parameters.Add(new NpgsqlParameter<int>
        {
            TypedValue = query.ItemId,
        });
        command.Parameters.Add(new NpgsqlParameter<int>
        {
            TypedValue = query.WorldId,
        });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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
                Live = true,
            });
        }

        return listings;
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