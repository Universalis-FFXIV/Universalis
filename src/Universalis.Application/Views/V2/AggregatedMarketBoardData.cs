using System.Collections.Generic;

namespace Universalis.Application.Views.V2;

public record AggregatedMarketBoardData(
    List<AggregatedMarketBoardData.Result> Results,
    List<int> FailedItems
)
{
    public record Result(
        int ItemId,
        AggregatedResult Nq,
        AggregatedResult Hq,
        List<WorldUploadTime> WorldUploadTimes
    );

    public record AggregatedResult(
        MinListing MinListing,
        MedianListing MedianListing,
        RecentPurchase RecentPurchase,
        AverageSalePrice AverageSalePrice,
        DailySaleVelocity DailySaleVelocity
    );

    public record MinListing(
        MinListing.Entry World,
        MinListing.Entry Dc,
        MinListing.Entry Region
    )
    {
        public record Entry(int Price, int? WorldId);
    }

    public record MedianListing(
        MedianListing.Entry World,
        MedianListing.Entry Dc,
        MedianListing.Entry Region
    )
    {
        public record Entry(int Price);
    }

    public record RecentPurchase(
        RecentPurchase.Entry World,
        RecentPurchase.Entry Dc,
        RecentPurchase.Entry Region
    )
    {
        public record Entry(int Price, long Timestamp, int? WorldId);
    }

    public record AverageSalePrice(
        AverageSalePrice.Entry World,
        AverageSalePrice.Entry Dc,
        AverageSalePrice.Entry Region
    )
    {
        public record Entry(double Price);
    }

    public record DailySaleVelocity(
        DailySaleVelocity.Entry World,
        DailySaleVelocity.Entry Dc,
        DailySaleVelocity.Entry Region
    )
    {
        public record Entry(double Quantity);
    }

    public record WorldUploadTime(
        int WorldId,
        long Timestamp
    );
}