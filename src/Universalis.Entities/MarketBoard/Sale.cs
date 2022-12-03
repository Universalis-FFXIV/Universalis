using Amazon.DynamoDBv2.DataModel;
using System;

namespace Universalis.Entities.MarketBoard;

[DynamoDBTable("sale_entry")]
public class Sale : IEquatable<Sale>
{
    [DynamoDBHashKey("id", typeof(GuidConverter))]
    public Guid Id { get; set; }

    [DynamoDBProperty("world_id")]
    [DynamoDBGlobalSecondaryIndexRangeKey("sale_entry_item_id_world_id")]
    public int WorldId { get; set; }

    [DynamoDBProperty("item_id")]
    [DynamoDBGlobalSecondaryIndexHashKey("sale_entry_item_id_world_id")]
    public int ItemId { get; set; }

    [DynamoDBProperty("hq")]
    public bool Hq { get; set; }

    [DynamoDBProperty("unit_price")]
    public int PricePerUnit { get; set; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    [DynamoDBProperty("quantity")]
    public int? Quantity { get; set; }

    // Names before May 22, 2022 weren't stored here, and therefore will be null
    [DynamoDBProperty("buyer_name")]
    public string BuyerName { get; set; }

    // Values before June 26, 2022 weren't stored here, and therefore will be null
    [DynamoDBProperty("on_mannequin")]
    public bool? OnMannequin { get; set; }

    [DynamoDBRangeKey("sale_time", typeof(UnixMsDateTimeConverter))]
    public DateTime SaleTime { get; set; }

    [DynamoDBProperty("uploader_id")]
    public string UploaderIdHash { get; set; }

    public bool Equals(Sale other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return WorldId == other.WorldId
               && ItemId == other.ItemId
               && Hq == other.Hq
               && PricePerUnit == other.PricePerUnit
               && Quantity == other.Quantity
               && BuyerName == other.BuyerName
               && SaleTime.Equals(other.SaleTime);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Sale)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(WorldId, ItemId, Hq, PricePerUnit, Quantity, BuyerName, SaleTime, UploaderIdHash);
    }

    public static bool operator ==(Sale left, Sale right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Sale left, Sale right)
    {
        return !Equals(left, right);
    }
}