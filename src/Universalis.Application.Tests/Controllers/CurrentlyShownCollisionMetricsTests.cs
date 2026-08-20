using System.Collections.Generic;
using Universalis.Application.Controllers;
using Universalis.Application.Views.V1;
using Xunit;

namespace Universalis.Application.Tests.Controllers;

public class CurrentlyShownCollisionMetricsTests
{
    [Fact]
    public void CountListingCollisions_DistinguishesSameWorldDuplicates()
    {
        var view = new CurrentlyShownView
        {
            Listings = new List<ListingView>
            {
                new() { ListingIdHash = "listing", WorldId = 74 },
                new() { ListingIdHash = "listing", WorldId = 74 },
            },
        };

        var collisions = CurrentlyShownControllerBase.CountListingCollisions(view, null);

        Assert.Equal(1, collisions.SameWorldExtra);
        Assert.Equal(0, collisions.CrossWorldExtra);
    }

    [Fact]
    public void CountListingCollisions_DistinguishesCrossWorldIds()
    {
        var view = new CurrentlyShownView
        {
            Listings = new List<ListingView>
            {
                new() { ListingIdHash = "listing", WorldId = 74 },
                new() { ListingIdHash = "listing", WorldId = 34 },
            },
        };

        var collisions = CurrentlyShownControllerBase.CountListingCollisions(view, null);

        Assert.Equal(0, collisions.SameWorldExtra);
        Assert.Equal(1, collisions.CrossWorldExtra);
    }

    [Fact]
    public void CountListingCollisions_UsesScopeWorldForWorldResponses()
    {
        var view = new CurrentlyShownView
        {
            Listings = new List<ListingView>
            {
                new() { ListingIdHash = "listing" },
                new() { ListingIdHash = "listing" },
            },
        };

        var collisions = CurrentlyShownControllerBase.CountListingCollisions(view, 74);

        Assert.Equal(1, collisions.SameWorldExtra);
        Assert.Equal(0, collisions.CrossWorldExtra);
    }
}
