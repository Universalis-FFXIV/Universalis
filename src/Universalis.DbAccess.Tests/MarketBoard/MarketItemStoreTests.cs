using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class MarketItemStoreTests : IClassFixture<DbFixture>
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
        var result = await store.Retrieve(93, 5);

        Assert.Equal(marketItem.WorldId, result.WorldId);
        Assert.Equal(marketItem.ItemId, result.ItemId);
        Assert.Equal(marketItem.LastUploadTime, result.LastUploadTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertUpdateRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 93,
            ItemId = 5333,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var updatedTime = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        await store.Insert(marketItem);
        marketItem.LastUploadTime = updatedTime;
        await store.Update(marketItem);
        var result = await store.Retrieve(93, 5333);

        Assert.Equal(marketItem.WorldId, result.WorldId);
        Assert.Equal(marketItem.ItemId, result.ItemId);
        Assert.Equal(marketItem.LastUploadTime, result.LastUploadTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Update_Missing_Inserts()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 93,
            ItemId = 32000,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        
        await store.Update(marketItem);
        var result = await store.Retrieve(93, 32000);

        Assert.Equal(marketItem.WorldId, result.WorldId);
        Assert.Equal(marketItem.ItemId, result.ItemId);
        Assert.Equal(marketItem.LastUploadTime, result.LastUploadTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var result = await store.Retrieve(93, 4);

        Assert.Null(result);
    }
}
