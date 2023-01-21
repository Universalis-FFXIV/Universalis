using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

[Collection("Database collection")]
public class MarketItemStoreTests
{
    private readonly DbFixture _fixture;

    public MarketItemStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.Insert(marketItem);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Multiple_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.Insert(marketItem);
        await store.Insert(marketItem);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 93,
            ItemId = 5,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.Insert(marketItem);
        await Task.Delay(1000);
        var result = await store.Retrieve(new MarketItemQuery { WorldId = 93, ItemId = 5 });

        Assert.NotNull(result);
        Assert.Equal(marketItem.WorldId, result.WorldId);
        Assert.Equal(marketItem.ItemId, result.ItemId);
        Assert.Equal(marketItem.LastUploadTime, result.LastUploadTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieveMany_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var itemIds = Enumerable.Range(100, 105).ToList();
        var dateTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var itemId in itemIds)
        {
            var marketItem = new MarketItem
            {
                WorldId = 93,
                ItemId = itemId,
                LastUploadTime = dateTime,
            };

            await store.Insert(marketItem);
        }

        var results = await store.RetrieveMany(new MarketItemManyQuery { WorldIds = new[] { 93 }, ItemIds = itemIds });
        Assert.NotNull(results);

        foreach (var (itemId, result) in itemIds.Zip(results))
        {
            Assert.NotNull(result);
            Assert.Equal(93, result.WorldId);
            Assert.Equal(itemId, result.ItemId);
            Assert.Equal(dateTime, result.LastUploadTime);
        }
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var result = await store.Retrieve(new MarketItemQuery { WorldId = 93, ItemId = 4 });

        Assert.Null(result);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveMany_Missing_ReturnsEmpty()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var result = await store.RetrieveMany(new MarketItemManyQuery
            { WorldIds = new[] { 93 }, ItemIds = Enumerable.Range(200, 210) });

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}