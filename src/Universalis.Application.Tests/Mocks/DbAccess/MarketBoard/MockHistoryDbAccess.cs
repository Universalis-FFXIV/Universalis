using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockHistoryDbAccess : IHistoryDbAccess
{
    private readonly Dictionary<Guid, Sale> _collection = new();

    public Task Create(History document, CancellationToken cancellationToken = default)
    {
        return InsertSales(document.Sales, new HistoryQuery { WorldId = document.WorldId, ItemId = document.ItemId },
            cancellationToken);
    }

    public Task<History> Retrieve(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        var sales = _collection
            .Select(h => h.Value)
            .Where(sale => sale.WorldId == query.WorldId && sale.ItemId == query.ItemId)
            .OrderByDescending(sale => sale.SaleTime)
            .Take(query.Count ?? 1000)
            .ToList();
        if (sales.Count == 0)
        {
            return Task.FromResult<History>(null);
        }

        return Task.FromResult(new History
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTimeUnixMilliseconds =
                sales.Count == 0 ? 0 : new DateTimeOffset(sales[0].SaleTime).ToUnixTimeMilliseconds(),
            Sales = sales,
        });
    }

    public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query,
        CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(query.WorldIds
                .SelectMany(worldId => query.ItemIds.Select(itemId =>
                    Retrieve(new HistoryQuery { WorldId = worldId, ItemId = itemId }, cancellationToken)))))
            .Where(o => o != null);
    }

    public Task InsertSales(IEnumerable<Sale> sales, HistoryQuery query, CancellationToken cancellationToken = default)
    {
        foreach (var sale in sales)
        {
            _collection[sale.Id] = sale;
        }

        return Task.CompletedTask;
    }
}