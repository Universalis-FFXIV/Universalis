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
    public async Task SetData_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.SetData(marketItem);
    }
    
#if DEBUG
    [Fact]
#endif
    public async Task SetData_Multiple_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.SetData(marketItem);
        await store.SetData(marketItem);
    }

#if DEBUG
    [Fact]
#endif
    public async Task SetDataGetData_Works()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var marketItem = new MarketItem
        {
            WorldId = 93,
            ItemId = 5,
            LastUploadTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await store.SetData(marketItem);
        await Task.Delay(1000);
        var result = await store.GetData(93, 5);

        Assert.NotNull(result);
        Assert.Equal(marketItem.WorldId, result.WorldId);
        Assert.Equal(marketItem.ItemId, result.ItemId);
        Assert.Equal(marketItem.LastUploadTime, result.LastUploadTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetData_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<IMarketItemStore>();
        var result = await store.GetData(93, 4);

        Assert.Null(result);
    }
}
