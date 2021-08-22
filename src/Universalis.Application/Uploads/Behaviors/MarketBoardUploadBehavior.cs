using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;
using Listing = Universalis.Entities.MarketBoard.Listing;
using Materia = Universalis.Entities.Materia;
using Sale = Universalis.Entities.MarketBoard.Sale;

namespace Universalis.Application.Uploads.Behaviors
{
    public class MarketBoardUploadBehavior : IUploadBehavior
    {
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IHistoryDbAccess _historyDb;

        public MarketBoardUploadBehavior(ICurrentlyShownDbAccess currentlyShownDb, IHistoryDbAccess historyDb)
        {
            _currentlyShownDb = currentlyShownDb;
            _historyDb = historyDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return parameters.WorldId != null
                   && parameters.ItemId != null
                   && !string.IsNullOrEmpty(parameters.UploaderId)
                   && (parameters.Sales != null || parameters.Listings != null);
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
                if (Util.HasHtmlTags(JsonSerializer.Serialize(parameters.Sales)))
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
                    })
                    .Where(s => s.PricePerUnit > 0)
                    .Where(s => s.Quantity > 0)
                    .Where(s => s.TimestampUnixSeconds > 0)
                    .ToList();
                cleanSales.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

                var existingHistory = await _historyDb.Retrieve(new HistoryQuery
                {
                    WorldId = worldId,
                    ItemId = itemId,
                });
                var minimizedSales = cleanSales.Select(s => MinimizedSale.FromSale(s, parameters.UploaderId)).ToList();

                var historyDocument = new History
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                };

                if (existingHistory == null)
                {
                    historyDocument.Sales = minimizedSales;
                    await _historyDb.Create(historyDocument);
                }
                else
                {
                    // Remove duplicates
                    var head = existingHistory.Sales.FirstOrDefault();
                    for (var i = 0; i < minimizedSales.Count; i++)
                    {
                        if (minimizedSales.Count == 0) break;
                        if (minimizedSales[0].Equals(head))
                        {
                            break;
                        }

                        existingHistory.Sales.Insert(0, minimizedSales[i]);
                        minimizedSales.RemoveAt(0);
                    }

                    // Trims out duplicates and any invalid data
                    existingHistory.Sales = await existingHistory.Sales
                        .ToAsyncEnumerable()
                        //.Where(s => s.PricePerUnit > 0) // We check PPU and *not* quantity because there are entries from before quantity was tracked
                        .Distinct()
                        .ToListAsync();
                    existingHistory.Sales.Sort((a, b) => (int)Math.Truncate(b.SaleTimeUnixSeconds - a.SaleTimeUnixSeconds));

                    historyDocument.Sales = existingHistory.Sales;
                    await _historyDb.Update(historyDocument, new HistoryQuery
                    {
                        WorldId = worldId,
                        ItemId = itemId,
                    });
                }
            }

            List<Listing> cleanListings = null;
            if (parameters.Listings != null)
            {
                if (Util.HasHtmlTags(JsonSerializer.Serialize(parameters.Listings)))
                {
                    return new BadRequestResult();
                }

                cleanListings = parameters.Listings
                    .Select(l =>
                    {
                        return new Listing
                        {
                            ListingIdInternal = l.ListingId,
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
                            CreatorIdInternal = Util.ParseUnusualId(l.CreatorId),
                            CreatorName = l.CreatorName,
                            LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds,
                            RetainerIdInternal = Util.ParseUnusualId(l.RetainerId),
                            RetainerName = l.RetainerName,
                            RetainerCityIdInternal = l.RetainerCityId,
                            SellerIdInternal = Util.ParseUnusualId(l.SellerId),
                            UploadApplicationName = source.Name,
                        };
                    })
                    .ToList();
                cleanListings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
            }

            var existingCurrentlyShown = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            });

            var document = new CurrentlyShown
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
                Listings = cleanListings ?? existingCurrentlyShown?.Listings ?? new List<Listing>(),
                RecentHistory = cleanSales ?? existingCurrentlyShown?.RecentHistory ?? new List<Sale>(),
                UploaderIdHash = parameters.UploaderId,
            };

            await _currentlyShownDb.Update(document, new CurrentlyShownQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            });

            return null;
        }
    }
}