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

        var listingIds = new List<string>();
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
            listingIds.Add(reader.GetString(0));
        }

        await using var killOld = new NpgsqlCommand("UPDATE listing SET live = FALSE WHERE listing_id != ANY($1)", connection, transaction)
        {
            Parameters =
            {
                new NpgsqlParameter<string[]> { TypedValue = listingIds.ToArray() },
            },
        };

        await killOld.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT listing_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, creator_id, creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, seller_id FROM listing WHERE item_id = $1 AND world_id = $2 AND live ORDER BY unit_price");
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
                ItemId = query.ItemId,
                WorldId = query.WorldId,
                Live = true,
                ListingId = reader.GetString(0),
                Hq = reader.GetBoolean(1),
                OnMannequin = reader.GetBoolean(2),
                Materia = ConvertMateriaFromJArray(reader.GetFieldValue<JArray>(3)),
                PricePerUnit = reader.GetInt32(4),
                Quantity = reader.GetInt32(5),
                DyeId = reader.GetInt32(6),
                CreatorId = reader.GetString(7),
                CreatorName = reader.GetString(8),
                LastReviewTime = reader.GetDateTime(9),
                RetainerId = reader.GetString(10),
                RetainerName = reader.GetString(11),
                RetainerCityId = reader.GetInt32(12),
                SellerId = reader.GetString(13),
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