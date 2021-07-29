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
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;
using Materia = Universalis.Entities.Materia;
using Sale = Universalis.Entities.MarketBoard.Sale;

namespace Universalis.Application.Uploads.Behaviors
{
    public class CurrentlyShownUploadBehavior : IUploadBehavior
    {
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IHistoryDbAccess _historyDb;

        public CurrentlyShownUploadBehavior(ICurrentlyShownDbAccess currentlyShownDb, IHistoryDbAccess historyDb)
        {
            _currentlyShownDb = currentlyShownDb;
            _historyDb = historyDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return parameters.WorldId != null && parameters.ItemId != null &&
                   (parameters.Sales != null || parameters.Listings != null);
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters)
        {
            // ReSharper disable PossibleInvalidOperationException
            var worldId = parameters.WorldId.Value;
            var itemId = parameters.ItemId.Value;
            // ReSharper restore PossibleInvalidOperationException

            List<Sale> cleanSales = null;
            if (parameters.Sales != null)
            {
                if (Util.HasHtmlTags(JsonConvert.SerializeObject(parameters.Sales)))
                {
                    return new BadRequestResult();
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
                    WorldId = worldId,
                    ItemId = itemId,
                });
                var minimizedSales = cleanSales.Select(MinimizedSale.FromSale).ToList();

                var document = new History
                {
                    WorldId = worldId,
                    ItemId = itemId,
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
                        WorldId = worldId,
                        ItemId = itemId,
                    });
                }
            }

            if (parameters.Listings != null)
            {
                if (Util.HasHtmlTags(JsonConvert.SerializeObject(parameters.Listings)))
                {
                    return new BadRequestResult();
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

                        return new Entities.MarketBoard.Listing
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
                    WorldId = worldId,
                    ItemId = itemId,
                });

                var document = new CurrentlyShown
                {
                    WorldId = worldId,
                    ItemId = itemId,
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
                        WorldId = worldId,
                        ItemId = itemId,
                    });
                }
            }

            return null;
        }
    }
}