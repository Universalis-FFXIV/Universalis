using System.Collections.Generic;

namespace Universalis.Application.Views.V2;

public record AggregatedMarketBoardData(
    List<AggregatedMarketBoardData.Result> Results,
    List<int> FailedItems
)
{
    public record Result
    {
        public required int ItemId { get; init; }
        public required AggregatedResult Nq { get; init; }
        public required AggregatedResult Hq { get; init; }
        public required List<WorldUploadTime> WorldUploadTimes { get; init; }
    }

    public record AggregatedResult
    {
        public required MinListing MinListing { get; init; }
        public MedianListing MedianListing { get; init; }
        public required RecentPurchase RecentPurchase { get; init; }
        public required AverageSalePrice AverageSalePrice { get; init; }
        public required DailySaleVelocity DailySaleVelocity { get; init; }
    }

    public record MinListing
    {
        public required Entry World { get; init; }
        public required Entry Dc { get; init; }
        public required Entry Region { get; init; }

        public record Entry(int Price, int? WorldId);
    }

    public record MedianListing
    {
        public required Entry World { get; init; }
        public required Entry Dc { get; init; }
        public required Entry Region { get; init; }

        public record Entry(int Price);
    }

    public record RecentPurchase
    {
        public required Entry World { get; init; }
        public required Entry Dc { get; init; }
        public required Entry Region { get; init; }

        public record Entry
        {
            public required int Price { get; init; }
            public required long Timestamp { get; init; }
            public int? WorldId { get; init; }
        }
    }

    public record AverageSalePrice
    {
        public required Entry World { get; init; }
        public required Entry Dc { get; init; }
        public required Entry Region { get; init; }

        public record Entry(double Price);
    }

    public record DailySaleVelocity
    {
        public required Entry World { get; init; }
        public required Entry Dc { get; init; }
        public required Entry Region { get; init; }

        public record Entry(double Quantity);
    }

    public record WorldUploadTime(
        int WorldId,
        long Timestamp
    );
}
