using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.UploadSchema;
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

            if (parameters.WorldId != null && parameters.ItemId != null && parameters.Sales != null)
            {
                if (Util.HasHtmlTags(JsonConvert.SerializeObject(parameters.Sales)))
                {
                    return BadRequest();
                }

                var cleanSales = parameters.Sales
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

                if (existingHistory == null)
                {
                    await _historyDb.Create(new History
                    {
                        WorldId = parameters.WorldId.Value,
                        ItemId = parameters.ItemId.Value,
                        LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                        Sales = minimizedSales,
                    });
                }
                else
                {
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

                    await _historyDb.Update(new History
                    {
                        WorldId = parameters.WorldId.Value,
                        ItemId = parameters.ItemId.Value,
                        LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                        Sales = existingHistory.Sales,
                    }, new HistoryQuery
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

            return Ok(); // TODO
        }
    }
}
