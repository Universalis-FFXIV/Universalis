using Amazon.DynamoDBv2.DataModel;
using System;

namespace Universalis.Entities.MarketBoard;

[DynamoDBTable("sale_entry")]
public class Sale : IEquatable<Sale>
{
    [DynamoDBHashKey]
    [DynamoDBProperty("id", typeof(GuidConverter))]
    public Guid Id { get; init; }

    [DynamoDBProperty("world_id")]
    public uint WorldId { get; init; }

    [DynamoDBProperty("item_id")]
    public uint ItemId { get; init; }

    [DynamoDBProperty("hq")]
    public bool Hq { get; init; }

    [DynamoDBProperty("unit_price")]
    public uint PricePerUnit { get; init; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    [DynamoDBProperty("quantity")]
    public uint? Quantity { get; init; }

    // Names before May 22, 2022 weren't stored here, and therefore will be null
    [DynamoDBProperty("buyer_name")]
    public string BuyerName { get; init; }

    // Values before June 26, 2022 weren't stored here, and therefore will be null
    [DynamoDBProperty("on_mannequin")]
    public bool? OnMannequin { get; init; }

    [DynamoDBRangeKey]
    [DynamoDBProperty("sale_time", typeof(UnixMsDateTimeConverter))]
    public DateTime SaleTime { get; init; }

    [DynamoDBProperty("uploader_id")]
    public string UploaderIdHash { get; init; }

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