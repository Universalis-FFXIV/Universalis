using Universalis.Application.Realtime.Messages;
using Xunit;

namespace Universalis.Application.Tests.Realtime.Messages;

/// <summary>
/// Tests for the IFilterableMessage interface implementation and filter-based message matching.
/// These tests verify that the interface-based filter extraction works correctly,
/// replacing the previous reflection-based approach for better performance.
/// </summary>
public class EventConditionFilterTests
{
    // === GetFilterValues() Implementation Tests ===

    [Fact]
    public void ItemUpdate_GetFilterValues_ReturnsItemAndWorld()
    {
        var message = new ItemUpdate { ItemId = 5, WorldId = 74 };
        var filters = message.GetFilterValues();

        Assert.Equal("5", filters["item"]);
        Assert.Equal("74", filters["world"]);
    }

    [Fact]
    public void ItemUpdate_GetFilterValues_CachesResult()
    {
        var message = new ItemUpdate { ItemId = 5, WorldId = 74 };
        var filters1 = message.GetFilterValues();
        var filters2 = message.GetFilterValues();

        Assert.Same(filters1, filters2); // Same instance = cached
    }

    [Fact]
    public void ListingsAdd_GetFilterValues_ReturnsItemAndWorld()
    {
        var message = new ListingsAdd { ItemId = 10, WorldId = 50, Listings = [] };
        var filters = message.GetFilterValues();

        Assert.Equal("10", filters["item"]);
        Assert.Equal("50", filters["world"]);
    }

    [Fact]
    public void ListingsAdd_GetFilterValues_CachesResult()
    {
        var message = new ListingsAdd { ItemId = 10, WorldId = 50, Listings = [] };
        var filters1 = message.GetFilterValues();
        var filters2 = message.GetFilterValues();

        Assert.Same(filters1, filters2);
    }

    [Fact]
    public void ListingsRemove_GetFilterValues_ReturnsItemAndWorld()
    {
        var message = new ListingsRemove { ItemId = 15, WorldId = 60, Listings = [] };
        var filters = message.GetFilterValues();

        Assert.Equal("15", filters["item"]);
        Assert.Equal("60", filters["world"]);
    }

    [Fact]
    public void ListingsRemove_GetFilterValues_CachesResult()
    {
        var message = new ListingsRemove { ItemId = 15, WorldId = 60, Listings = [] };
        var filters1 = message.GetFilterValues();
        var filters2 = message.GetFilterValues();

        Assert.Same(filters1, filters2);
    }

    [Fact]
    public void SalesAdd_GetFilterValues_ReturnsItemAndWorld()
    {
        var message = new SalesAdd { ItemId = 20, WorldId = 30, Sales = [] };
        var filters = message.GetFilterValues();

        Assert.Equal("20", filters["item"]);
        Assert.Equal("30", filters["world"]);
    }

    [Fact]
    public void SalesAdd_GetFilterValues_CachesResult()
    {
        var message = new SalesAdd { ItemId = 20, WorldId = 30, Sales = [] };
        var filters1 = message.GetFilterValues();
        var filters2 = message.GetFilterValues();

        Assert.Same(filters1, filters2);
    }

    [Fact]
    public void SubscribeFailure_GetFilterValues_ReturnsEmpty()
    {
        var message = new SubscribeFailure("error");
        var filters = message.GetFilterValues();

        Assert.Empty(filters);
    }

    // === ShouldSend() Integration Tests with Real Message Types ===

    [Fact]
    public void ShouldSend_NoFilters_MatchesOnChannel()
    {
        var condition = EventCondition.Parse("listings/add");
        var message = new ListingsAdd { ItemId = 5, WorldId = 74, Listings = [] };

        Assert.True(condition.ShouldSend(message));
    }

    [Fact]
    public void ShouldSend_NoFilters_DoesNotMatchWrongChannel()
    {
        var condition = EventCondition.Parse("sales/add");
        var message = new ListingsAdd { ItemId = 5, WorldId = 74, Listings = [] };

        Assert.False(condition.ShouldSend(message));
    }

    [Fact]
    public void ShouldSend_WithItemFilter_MatchesCorrectItem()
    {
        var condition = EventCondition.Parse("listings/add{item=5}");
        var matchingMessage = new ListingsAdd { ItemId = 5, WorldId = 74, Listings = [] };
        var nonMatchingMessage = new ListingsAdd { ItemId = 10, WorldId = 74, Listings = [] };

        Assert.True(condition.ShouldSend(matchingMessage));
        Assert.False(condition.ShouldSend(nonMatchingMessage));
    }

    [Fact]
    public void ShouldSend_WithWorldFilter_MatchesCorrectWorld()
    {
        var condition = EventCondition.Parse("sales/add{world=74}");
        var matchingMessage = new SalesAdd { ItemId = 5, WorldId = 74, Sales = [] };
        var nonMatchingMessage = new SalesAdd { ItemId = 5, WorldId = 50, Sales = [] };

        Assert.True(condition.ShouldSend(matchingMessage));
        Assert.False(condition.ShouldSend(nonMatchingMessage));
    }

    [Fact]
    public void ShouldSend_WithMultipleFilters_RequiresAllMatch()
    {
        var condition = EventCondition.Parse("item/update{item=5, world=74}");
        var fullMatch = new ItemUpdate { ItemId = 5, WorldId = 74 };
        var partialMatch1 = new ItemUpdate { ItemId = 5, WorldId = 50 };
        var partialMatch2 = new ItemUpdate { ItemId = 10, WorldId = 74 };
        var noMatch = new ItemUpdate { ItemId = 10, WorldId = 50 };

        Assert.True(condition.ShouldSend(fullMatch));
        Assert.False(condition.ShouldSend(partialMatch1));
        Assert.False(condition.ShouldSend(partialMatch2));
        Assert.False(condition.ShouldSend(noMatch));
    }

    [Fact]
    public void ShouldSend_WrongChannel_ReturnsFalse()
    {
        var condition = EventCondition.Parse("listings/add{item=5}");
        var wrongChannel = new SalesAdd { ItemId = 5, WorldId = 74, Sales = [] };

        Assert.False(condition.ShouldSend(wrongChannel));
    }

    [Fact]
    public void ShouldSend_UnknownFilterKey_ReturnsFalse()
    {
        // Condition asks for filter that message doesn't provide
        var condition = EventCondition.Parse("listings/add{unknown=value}");
        var message = new ListingsAdd { ItemId = 5, WorldId = 74, Listings = [] };

        Assert.False(condition.ShouldSend(message));
    }

    [Fact]
    public void ShouldSend_ListingsRemove_WorksWithFilters()
    {
        var condition = EventCondition.Parse("listings/remove{item=100, world=200}");
        var matchingMessage = new ListingsRemove { ItemId = 100, WorldId = 200, Listings = [] };
        var nonMatchingMessage = new ListingsRemove { ItemId = 100, WorldId = 201, Listings = [] };

        Assert.True(condition.ShouldSend(matchingMessage));
        Assert.False(condition.ShouldSend(nonMatchingMessage));
    }

    [Fact]
    public void ShouldSend_ItemUpdate_WorksWithFilters()
    {
        var condition = EventCondition.Parse("item/update{world=74}");
        var matchingMessage = new ItemUpdate { ItemId = 999, WorldId = 74 };
        var nonMatchingMessage = new ItemUpdate { ItemId = 999, WorldId = 75 };

        Assert.True(condition.ShouldSend(matchingMessage));
        Assert.False(condition.ShouldSend(nonMatchingMessage));
    }

    [Fact]
    public void ShouldSend_SubscribeFailure_MatchesOnChannelOnly()
    {
        // SubscribeFailure has no filters, so it should only match on channel
        var condition = EventCondition.Parse("subscribe/error");
        var message = new SubscribeFailure("test error");

        Assert.True(condition.ShouldSend(message));
    }

    [Fact]
    public void ShouldSend_SubscribeFailure_WithFilter_ReturnsFalse()
    {
        // SubscribeFailure provides no filter values, so any filter should fail
        var condition = EventCondition.Parse("subscribe/error{reason=test}");
        var message = new SubscribeFailure("test");

        Assert.False(condition.ShouldSend(message));
    }

    // === Filter Value Edge Cases ===

    [Fact]
    public void GetFilterValues_ZeroValues_ReturnCorrectStrings()
    {
        var message = new ItemUpdate { ItemId = 0, WorldId = 0 };
        var filters = message.GetFilterValues();

        Assert.Equal("0", filters["item"]);
        Assert.Equal("0", filters["world"]);
    }

    [Fact]
    public void GetFilterValues_LargeValues_ReturnCorrectStrings()
    {
        var message = new ItemUpdate { ItemId = int.MaxValue, WorldId = int.MaxValue };
        var filters = message.GetFilterValues();

        Assert.Equal(int.MaxValue.ToString(), filters["item"]);
        Assert.Equal(int.MaxValue.ToString(), filters["world"]);
    }

    [Fact]
    public void ShouldSend_FilterWithSpaces_ParsesCorrectly()
    {
        // Test that filter parsing handles spaces correctly
        var condition = EventCondition.Parse("listings/add{item = 5, world = 74}");
        var message = new ListingsAdd { ItemId = 5, WorldId = 74, Listings = [] };

        Assert.True(condition.ShouldSend(message));
    }
}
