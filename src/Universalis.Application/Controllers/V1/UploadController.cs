using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;
using Universalis.GameData;
using Listing = Universalis.Entities.MarketBoard.Listing;
using Materia = Universalis.Entities.Materia;
using Sale = Universalis.Entities.MarketBoard.Sale;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/upload/{apiKey}")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IHistoryDbAccess _historyDb;
        private readonly IContentDbAccess _contentDb;
        private readonly ITaxRatesDbAccess _taxRatesDb;
        private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
        private readonly IWorldUploadCountDbAccess _worldUploadCountDb;
        private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;

        public UploadController(
            IGameDataProvider gameData,
            ITrustedSourceDbAccess trustedSourceDb,
            ICurrentlyShownDbAccess currentlyShownDb,
            IHistoryDbAccess historyDb,
            IContentDbAccess contentDb,
            ITaxRatesDbAccess taxRatesDb,
            IFlaggedUploaderDbAccess flaggedUploaderDb,
            IWorldUploadCountDbAccess worldUploadCountDb,
            IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
        {
            _gameData = gameData;
            _trustedSourceDb = trustedSourceDb;
            _currentlyShownDb = currentlyShownDb;
            _historyDb = historyDb;
            _contentDb = contentDb;
            _taxRatesDb = taxRatesDb;
            _flaggedUploaderDb = flaggedUploaderDb;
            _worldUploadCountDb = worldUploadCountDb;
            _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
        }

        [HttpPost]
        public async Task<IActionResult> Post(string apiKey, [FromBody] UploadParameters parameters)
        {
            TrustedSource source;
            using (var sha256 = SHA256.Create())
            {
                await using var authStream = new MemoryStream(Encoding.UTF8.GetBytes(apiKey));
                source = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
                {
                    ApiKeySha256 = BitConverter.ToString(await sha256.ComputeHashAsync(authStream)),
                });
            }

            if (source == null)
            {
                return Forbid();
            }

            // Hash the uploader ID
            using (var sha256 = SHA256.Create())
            {
                await using var uploaderIdStream = new MemoryStream(Encoding.UTF8.GetBytes(parameters.UploaderId));
                parameters.UploaderId = BitConverter.ToString(await sha256.ComputeHashAsync(uploaderIdStream));
            }

            // Check if this uploader is flagged, cancel if they are
            if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderId = parameters.UploaderId }) !=
                null)
            {
                return Ok("Success");
            }

            // Store data
            if (parameters.WorldId != null)
            {
                if (!_gameData.AvailableWorldIds().Contains(parameters.WorldId.Value))
                    return NotFound(parameters.WorldId);

                var worldName = _gameData.AvailableWorlds()[parameters.WorldId.Value];
                await _worldUploadCountDb.Increment(new WorldUploadCountQuery { WorldName = worldName });
            }

            if (parameters.ItemId != null)
            {
                await _recentlyUpdatedItemsDb.Push(parameters.ItemId.Value);
            }

            List<Sale> cleanSales = null;
            if (parameters.WorldId != null && parameters.ItemId != null && parameters.Sales != null)
            {
                if (Util.HasHtmlTags(JsonConvert.SerializeObject(parameters.Sales)))
                {
                    return BadRequest();
                }

                cleanSales = parameters.Sales
                    .Select(s => new Sale
                    {
                        Hq = Util.ParseUnusualBool(s.Hq),
                        BuyerName = s.BuyerName,
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity,
                        TimestampUnixSeconds = s.TimestampUnixSeconds,
                        UploadApplicationName = source.Name,
                        UploaderIdHash = parameters.UploaderId,
                    })
                    .ToList();
                cleanSales.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

                var existingHistory = await _historyDb.Retrieve(new HistoryQuery
                {
                    WorldId = parameters.WorldId.Value,
                    ItemId = parameters.ItemId.Value,
                });
                var minimizedSales = cleanSales.Select(MinimizedSale.FromSale).ToList();

                var document = new History
                {
                    WorldId = parameters.WorldId.Value,
                    ItemId = parameters.ItemId.Value,
                    LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                };

                if (existingHistory == null)
                {
                    document.Sales = minimizedSales;
                    await _historyDb.Create(document);
                }
                else
                {
                    // Remove duplicates
                    var head = existingHistory.Sales.FirstOrDefault();
                    for (var i = 0; i < minimizedSales.Count; i++)
                    {
                        if (minimizedSales.Count == 0) break;
                        if (minimizedSales[0] == head)
                        {
                            break;
                        }

                        existingHistory.Sales.Insert(0, minimizedSales[i]);
                        minimizedSales.RemoveAt(0);
                    }

                    document.Sales = existingHistory.Sales;
                    await _historyDb.Update(document, new HistoryQuery
                    {
                        WorldId = parameters.WorldId.Value,
                        ItemId = parameters.ItemId.Value,
                    });
                }
            }

            if (parameters.WorldId != null && parameters.ItemId != null && parameters.Listings != null)
            {
                if (Util.HasHtmlTags(JsonConvert.SerializeObject(parameters.Listings)))
                {
                    return BadRequest();
                }

                var cleanListings = parameters.Listings
                    .Select(l =>
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            using var creatorIdStream = new MemoryStream(Encoding.UTF8.GetBytes(l.CreatorId));
                            l.CreatorId = BitConverter.ToString(sha256.ComputeHash(creatorIdStream));
                        }

                        using (var sha256 = SHA256.Create())
                        {
                            using var sellerIdStream = new MemoryStream(Encoding.UTF8.GetBytes(l.SellerId));
                            l.SellerId = BitConverter.ToString(sha256.ComputeHash(sellerIdStream));
                        }

                        return new Listing
                        {
                            ListingId = l.ListingId,
                            Hq = Util.ParseUnusualBool(l.Hq),
                            OnMannequin = Util.ParseUnusualBool(l.OnMannequin),
                            Materia = l.Materia.Select(s => new Materia
                            {
                                SlotId = s.SlotId,
                                MateriaId = s.MateriaId,
                            })
                                .ToList(),
                            PricePerUnit = l.PricePerUnit,
                            Quantity = l.Quantity,
                            DyeId = l.DyeId,
                            CreatorIdHash = l.CreatorId,
                            CreatorName = l.CreatorName,
                            LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds,
                            RetainerId = l.RetainerId,
                            RetainerName = l.RetainerName,
                            RetainerCityId = l.RetainerCityId,
                            SellerIdHash = l.SellerId,
                            UploadApplicationName = source.Name,
                        };
                    })
                    .ToList();
                cleanListings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);

                var existingCurrentlyShown = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
                {
                    WorldId = parameters.WorldId.Value,
                    ItemId = parameters.ItemId.Value,
                });

                var document = new CurrentlyShown
                {
                    WorldId = parameters.WorldId.Value,
                    ItemId = parameters.ItemId.Value,
                    LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                    Listings = cleanListings,
                    UploaderIdHash = parameters.UploaderId,
                };

                if (existingCurrentlyShown == null)
                {
                    document.RecentHistory = cleanSales ?? new List<Sale>();
                    await _currentlyShownDb.Create(document);
                }
                else
                {
                    document.RecentHistory = cleanSales ?? existingCurrentlyShown.RecentHistory;
                    await _currentlyShownDb.Update(document, new CurrentlyShownQuery
                    {
                        WorldId = parameters.WorldId.Value,
                        ItemId = parameters.ItemId.Value,
                    });
                }
            }

            if (parameters.WorldId != null && parameters.TaxRates != null)
            {
                await _taxRatesDb.Update(new TaxRates
                {
                    LimsaLominsa = parameters.TaxRates.LimsaLominsa,
                    Gridania = parameters.TaxRates.Gridania,
                    Uldah = parameters.TaxRates.Uldah,
                    Ishgard = parameters.TaxRates.Ishgard,
                    Kugane = parameters.TaxRates.Kugane,
                    Crystarium = parameters.TaxRates.Crystarium,
                    UploaderIdHash = parameters.UploaderId,
                    WorldId = parameters.WorldId.Value,
                    UploadApplicationName = source.Name,
                }, new TaxRatesQuery
                {
                    WorldId = parameters.WorldId.Value,
                });
            }

            if (!string.IsNullOrEmpty(parameters.ContentId) && !string.IsNullOrEmpty(parameters.CharacterName))
            {
                await _contentDb.Update(new Content
                {
                    ContentId = parameters.ContentId,
                    ContentType = ContentKind.Player,
                    CharacterName = parameters.CharacterName,
                }, new ContentQuery
                {
                    ContentId = parameters.ContentId,
                });
            }

            return Ok("Success");
        }
    }
}
