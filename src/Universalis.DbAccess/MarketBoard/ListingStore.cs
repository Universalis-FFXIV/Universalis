#nullable enable
using System;
using System.Collections.Concurrent;
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
using RetrieveManyLiveResult =
    System.Collections.Generic.IDictionary<Universalis.DbAccess.WorldItemPair,
        System.Collections.Generic.IList<Universalis.Entities.MarketBoard.Listing>>;

namespace Universalis.DbAccess.MarketBoard;

public class ListingStore : IListingStore
{
    private readonly ILogger<ListingStore> _logger;
    private readonly ILogger<MultiplexedRetrieveManyLiveBatch> _multiplexerLogger;
    private readonly NpgsqlDataSource _dataSource;

    private MultiplexedRetrieveManyLiveBatch? _currentBatch;

    // ReSharper disable once ContextualLoggerProblem
    public ListingStore(NpgsqlDataSource dataSource, ILogger<ListingStore> logger,
        ILogger<MultiplexedRetrieveManyLiveBatch> multiplexerLogger)
    {
        _dataSource = dataSource;
        _logger = logger;
        _multiplexerLogger = multiplexerLogger;
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

    public async Task<RetrieveManyLiveResult> RetrieveManyLive(ListingManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveManyLive");

        Guid? id;
        var batch = _currentBatch;
        var enlist = batch?.EnlistToBatch(query, cancellationToken);
        try
        {
            // This is a bit awkward but it works
            if (batch == null || enlist == null || (id = await enlist) == null)
            {
                return await RetrieveManyLiveInternal(query, 5, cancellationToken);
            }
        }
        catch (ObjectDisposedException e)
        {
            // The semaphore (or something else?) was disposed while enlisting to
            // the batch; this sounds rare
            _logger.LogError(e, "An object was disposed while enlisting to a batch");
            return await RetrieveManyLiveInternal(query, 5, cancellationToken);
        }

        return await batch.RetrieveResults(id.Value, cancellationToken);
    }

    private async Task<RetrieveManyLiveResult> RetrieveManyLiveInternal(ListingManyQuery query, int maxRetries,
        CancellationToken cancellationToken = default)
    {
        while (maxRetries > 0)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new MultiplexedRetrieveManyLiveBatch(connection, _multiplexerLogger);
            _currentBatch = batch;

            var id = await batch.EnlistToBatch(query, cancellationToken);
            if (id == null)
            {
                maxRetries--;
                continue;
            }

            var result = await batch.RetrieveResults(id.Value, cancellationToken);
            _currentBatch = null;
            return result;
        }

        throw new InvalidOperationException($"Maximum number of retries exceeded: {maxRetries}");
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
            .Select(m => new Materia { SlotId = m["slot_id"]!.Value<int>(), MateriaId = m["materia_id"]!.Value<int>() })
            .ToList();
    }

    /// <summary>
    /// A version of RetrieveManyLive that multiplexes several calls into a single batched command.
    /// </summary>
    public class MultiplexedRetrieveManyLiveBatch : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<Guid, RetrieveManyLiveResult> _results;
        private readonly ConcurrentQueue<MultiplexedRetrieveManyLiveCallState> _callState;
        private readonly DateTimeOffset _deadline;
        private readonly SemaphoreSlim _lock;
        private readonly TaskCompletionSource<object> _cs;

        private readonly ILogger<MultiplexedRetrieveManyLiveBatch> _logger;
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlBatch _batch;

        private bool _running;

        private bool _disposed;

        public MultiplexedRetrieveManyLiveBatch(NpgsqlConnection connection,
            ILogger<MultiplexedRetrieveManyLiveBatch> logger)
        {
            _logger = logger;
            _connection = connection;
            _batch = new NpgsqlBatch(connection);

            _cs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _deadline = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(50);
            _lock = new SemaphoreSlim(1, 1);
            _callState = new ConcurrentQueue<MultiplexedRetrieveManyLiveCallState>();
            _results = new ConcurrentDictionary<Guid, RetrieveManyLiveResult>();
        }

        /// <summary>
        /// Enlist to the current batch, if possible. If this task manages to enlist
        /// successfully, an ID will be returned, which can be used to retrieve results
        /// later.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The result ID, or null if enlisting failed.</returns>
        public async Task<Guid?> EnlistToBatch(ListingManyQuery query, CancellationToken cancellationToken = default)
        {
            if (_disposed || !await _lock.WaitAsync(TimeSpan.FromMilliseconds(50), cancellationToken))
            {
                // The command is probably executing already
                return null;
            }

            try
            {
                if (_running)
                {
                    // The command is running, back off
                    return null;
                }

                var id = Guid.NewGuid();

                var worldIds = query.WorldIds.ToList();
                var itemIds = query.ItemIds.ToList();
                var worldItemPairs = worldIds.SelectMany(worldId =>
                        itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
                    .ToList();

                _callState.Enqueue(new MultiplexedRetrieveManyLiveCallState(id, worldItemPairs));
                foreach (var (worldId, itemId) in worldItemPairs)
                {
                    _batch.BatchCommands.Add(new NpgsqlBatchCommand(
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

                return id;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Waits for the command to complete and then retrieves the results for
        /// the provided ID.
        /// </summary>
        /// <param name="id">The ID to retrieve data for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<RetrieveManyLiveResult> RetrieveResults(Guid id,
            CancellationToken cancellationToken = default)
        {
            await Task.WhenAny(_cs.Task, Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await ExecuteCommand();
                }

                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken));

            return _results[id];
        }

        private async Task ExecuteCommand()
        {
            if (_disposed || !await _lock.WaitAsync(TimeSpan.FromMilliseconds(50)))
            {
                // The command is probably executing already
                return;
            }

            try
            {
                if (_running || DateTimeOffset.UtcNow < _deadline)
                {
                    return;
                }

                _running = true;

                await using var reader =
                    await _batch.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                // Read the batch command results, in order
                while (_callState.TryDequeue(out var context))
                {
                    _results[context.Id] = new Dictionary<WorldItemPair, IList<Listing>>();
                    var batchesRead = 0;
                    do
                    {
                        var key = context.Queries[batchesRead];
                        var batchResult = new List<Listing>();
                        try
                        {
                            while (await reader.ReadAsync())
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

                            _results[context.Id][key] = batchResult;

                            batchesRead++;
                            await reader.NextResultAsync();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e,
                                "Failed to retrieve listings in multiplexed command (world={}, item={})",
                                key.WorldId, key.ItemId);
                            _cs.SetException(e);
                            throw;
                        }
                    } while (batchesRead != context.Queries.Count);
                }

                _cs.SetResult(true);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            // This should only ever be called by the function that created this instance,
            // so synchronization shouldn't be necessary.
            if (!_disposed)
            {
                _disposed = true;
                
                GC.SuppressFinalize(this);

                _lock.Dispose();
                await _batch.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private record struct MultiplexedRetrieveManyLiveCallState(Guid Id, List<WorldItemPair> Queries);
    }
}