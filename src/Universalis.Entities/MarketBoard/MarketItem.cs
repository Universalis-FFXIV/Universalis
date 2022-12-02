using Amazon.DynamoDBv2.DataModel;
using System;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

[DynamoDBTable("market_item")]
public class MarketItem : ICopyable
{
    [DynamoDBHashKey("item_id")]
    public int ItemId { get; init; }

    [DynamoDBRangeKey("world_id")]
    public int WorldId { get; init; }

    [DynamoDBProperty("last_upload_time", typeof(UnixMsDateTimeConverter))]
    public DateTime LastUploadTime { get; set; }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}