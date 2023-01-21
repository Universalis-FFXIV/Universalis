using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

[Collection("Database collection")]
public class CurrentlyShownStoreTests
{
    private readonly DbFixture _fixture;

    public CurrentlyShownStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, 2);
        await store.Insert(currentlyShown);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, 3);
        await store.Insert(currentlyShown);
        var results = await store.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 3 });

        Assert.NotNull(results);
        Assert.Equal(currentlyShown.WorldId, results.WorldId);
        Assert.Equal(currentlyShown.ItemId, results.ItemId);
        Assert.Equal(currentlyShown.LastUploadTimeUnixMilliseconds / 1000,
            results.LastUploadTimeUnixMilliseconds / 1000);
        Assert.Equal(currentlyShown.UploadSource, results.UploadSource);
        Assert.All(currentlyShown.Listings.OrderBy(l => l.PricePerUnit).Zip(results.Listings), pair =>
        {
            var (expected, actual) = pair;
            Assert.Equal(expected.ListingId, actual.ListingId);
            Assert.Equal(expected.Hq, actual.Hq);
            Assert.Equal(expected.OnMannequin, actual.OnMannequin);
            Assert.Equal(expected.PricePerUnit, actual.PricePerUnit);
            Assert.Equal(expected.Quantity, actual.Quantity);
            Assert.Equal(expected.RetainerName, actual.RetainerName);
            Assert.Equal(expected.RetainerId, actual.RetainerId);
            Assert.Equal(expected.RetainerCityId, actual.RetainerCityId);
            Assert.Equal(expected.DyeId, actual.DyeId);
            Assert.Equal(expected.CreatorId, actual.CreatorId);
            Assert.Equal(expected.CreatorName, actual.CreatorName);
            Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
            Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
            Assert.Equal(expected.SellerId, actual.SellerId);
            Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
            Assert.Equal(expected.Source, actual.Source);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieveMany_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var itemIds = Enumerable.Range(100, 5).ToList();
        var expectedCurrentlyShown = new Dictionary<int, CurrentlyShown>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var itemId in itemIds)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
            expectedCurrentlyShown[itemId] = currentlyShown;
            await store.Insert(currentlyShown);
        }

        var results = await store.RetrieveMany(new CurrentlyShownManyQuery
        { WorldIds = new[] { 74 }, ItemIds = itemIds });
        Assert.NotNull(results);

        foreach (var (itemId, result) in itemIds.Zip(results))
        {
            Assert.NotNull(results);
            Assert.Equal(expectedCurrentlyShown[itemId].WorldId, result.WorldId);
            Assert.Equal(itemId, result.ItemId);
            Assert.Equal(expectedCurrentlyShown[itemId].UploadSource, result.UploadSource);
            Assert.All(expectedCurrentlyShown[itemId].Listings.OrderBy(l => l.PricePerUnit).Zip(result.Listings),
                pair =>
                {
                    var (expected, actual) = pair;
                    Assert.Equal(expected.ListingId, actual.ListingId);
                    Assert.Equal(expected.Hq, actual.Hq);
                    Assert.Equal(expected.OnMannequin, actual.OnMannequin);
                    Assert.Equal(expected.PricePerUnit, actual.PricePerUnit);
                    Assert.Equal(expected.Quantity, actual.Quantity);
                    Assert.Equal(expected.RetainerName, actual.RetainerName);
                    Assert.Equal(expected.RetainerId, actual.RetainerId);
                    Assert.Equal(expected.RetainerCityId, actual.RetainerCityId);
                    Assert.Equal(expected.DyeId, actual.DyeId);
                    Assert.Equal(expected.CreatorId, actual.CreatorId);
                    Assert.Equal(expected.CreatorName, actual.CreatorName);
                    Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                        new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
                    Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
                    Assert.Equal(expected.SellerId, actual.SellerId);
                    Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
                    Assert.Equal(expected.Source, actual.Source);
                });
        }
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_ReturnsNull_WhenMissing()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var results = await store.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 4 });
        Assert.Null(results);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveMany_ReturnsEmpty_WhenMissing()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var results = await store.RetrieveMany(new CurrentlyShownManyQuery
        { WorldIds = new[] { 74 }, ItemIds = Enumerable.Range(200, 10) });
        var resultsList = results.ToList();
        Assert.Empty(resultsList);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveMany_DoesNotThrow_WhenSomeEmpty()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var expectedCurrentlyShown = new Dictionary<int, CurrentlyShown>();
        var expectedItemIds = Enumerable.Range(300, 5).ToList();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var itemId in expectedItemIds)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
            expectedCurrentlyShown[itemId] = currentlyShown;
            await store.Insert(currentlyShown);
        }

        var resultsItemIds = Enumerable.Range(300, 7).ToList();
        var results = (await store.RetrieveMany(new CurrentlyShownManyQuery
        { WorldIds = new[] { 74 }, ItemIds = resultsItemIds })).ToList();
        Assert.NotNull(results);
        Assert.True(results.Count == expectedItemIds.Count);

        foreach (var (itemId, result) in expectedItemIds.Zip(results))
        {
            Assert.NotNull(result);
            Assert.Equal(itemId, result.ItemId);
            Assert.Equal(expectedCurrentlyShown[itemId].WorldId, result.WorldId);
            Assert.Equal(expectedCurrentlyShown[itemId].UploadSource, result.UploadSource);

            Assert.NotEmpty(result.Listings);
            Assert.All(expectedCurrentlyShown[itemId].Listings.OrderBy(l => l.PricePerUnit).Zip(result.Listings),
            pair =>
            {
                var (expected, actual) = pair;
                Assert.Equal(expected.ListingId, actual.ListingId);
                Assert.Equal(expected.Hq, actual.Hq);
                Assert.Equal(expected.OnMannequin, actual.OnMannequin);
                Assert.Equal(expected.PricePerUnit, actual.PricePerUnit);
                Assert.Equal(expected.Quantity, actual.Quantity);
                Assert.Equal(expected.RetainerName, actual.RetainerName);
                Assert.Equal(expected.RetainerId, actual.RetainerId);
                Assert.Equal(expected.RetainerCityId, actual.RetainerCityId);
                Assert.Equal(expected.DyeId, actual.DyeId);
                Assert.Equal(expected.CreatorId, actual.CreatorId);
                Assert.Equal(expected.CreatorName, actual.CreatorName);
                Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                    new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
                Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
                Assert.Equal(expected.SellerId, actual.SellerId);
                Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
                Assert.Equal(expected.Source, actual.Source);
            });
        }
    }
}