using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>, ICurrentlyShownDbAccess
{
    private readonly ICurrentlyShownStore _store;

    public CurrentlyShownDbAccess(IMongoClient client, ICurrentlyShownStore store) : base(client,
        Constants.DatabaseName, "recentData")
    {
        _store = store;
    }

    public CurrentlyShownDbAccess(IMongoClient client, string databaseName, ICurrentlyShownStore store) : base(client,
        databaseName, "recentData")
    {
        _store = store;
    }

    async Task<CurrentlyShownSimple> ICurrentlyShownDbAccess.Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken)
    {
        var data = await _store.GetData(query.WorldId, query.ItemId);
        if (data.LastUploadTimeUnixMilliseconds == 0)
        {
            // No data in store; read through MongoDB
            var dataMongo = await base.Retrieve(query, cancellationToken);

            CurrentlyShownSimple dataConverted = null;
            if (dataMongo != null)
            {
                dataConverted = ConvertToSimple(dataMongo);
                await _store.SetData(dataConverted);
            }

            return dataConverted;
        }

        return data;
    }

    public override async Task Update(CurrentlyShown document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        var dataConverted = ConvertToSimple(document);
        await Update(dataConverted, query, cancellationToken);
    }

    public Task Update(CurrentlyShownSimple document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        return _store.SetData(document);
    }

    private static CurrentlyShownSimple ConvertToSimple(CurrentlyShown data)
    {
        var source = data.Listings.Count == 0
            ? ""
            : data.Listings[0].UploadApplicationName ?? "";
        var listings = data.Listings
            .Select(l => new ListingSimple
            {
                ListingId = l.ListingId,
                Hq = l.Hq,
                OnMannequin = l.OnMannequin,
                Materia = l.Materia,
                PricePerUnit = l.PricePerUnit,
                Quantity = l.Quantity,
                DyeId = l.DyeId,
                CreatorId = l.CreatorId,
                CreatorName = l.CreatorName ?? "",
                LastReviewTimeUnixSeconds = Convert.ToInt64(l.LastReviewTimeUnixSeconds),
                RetainerId = l.RetainerId ?? "",
                RetainerName = l.RetainerName ?? "",
                RetainerCityId = l.RetainerCityId,
                SellerId = l.SellerId ?? "",
            })
            .ToList();
        var sales = data.RecentHistory
            .Select(s => new SaleSimple
            {
                Hq = s.Hq,
                PricePerUnit = s.PricePerUnit,
                Quantity = s.Quantity,
                BuyerName = s.BuyerName ?? "",
                TimestampUnixSeconds = Convert.ToInt64(s.TimestampUnixSeconds),
            })
            .ToList();
        return new CurrentlyShownSimple(data.WorldId, data.ItemId,
            Convert.ToInt64(data.LastUploadTimeUnixMilliseconds), source, listings, sales);
    }
}