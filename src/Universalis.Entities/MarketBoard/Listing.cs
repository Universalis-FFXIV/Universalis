using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Universalis.Entities.MarketBoard
{
    public class Listing : IEquatable<Listing>
    {
        [BsonElement("listingID")]
        public object ListingIdInternal { get; init; }

        [BsonIgnore]
        public string ListingId => DbCleaning.NullIfZeroString(DbCleaning.ReadAsString(ListingIdInternal));

        [BsonElement("hq")]
        public bool Hq { get; init; }

        [BsonElement("onMannequin")]
        public bool OnMannequin { get; init; }

        [BsonElement("materia")]
        public List<Materia> Materia { get; init; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [BsonElement("quantity")]
        public uint Quantity { get; init; }

        [BsonElement("stainID")]
        public uint DyeId { get; init; }

        [BsonElement("creatorID")]
        public object CreatorIdInternal { get; init; }

        [BsonIgnore]
        public string CreatorId => DbCleaning.NullIfZeroString(DbCleaning.ReadAsString(CreatorIdInternal));

        [BsonElement("creatorName")]
        public string CreatorName { get; init; }

        [BsonElement("lastReviewTime")]
        public double LastReviewTimeUnixSeconds { get; init; }

        [BsonElement("retainerID")]
        public object RetainerIdInternal { get; init; }

        [BsonIgnore]
        public string RetainerId => DbCleaning.NullIfZeroString(DbCleaning.ReadAsString(RetainerIdInternal));

        [BsonElement("retainerName")]
        public string RetainerName { get; init; }

        [BsonElement("retainerCity")]
        public object RetainerCityIdInternal { get; init; }

        [BsonIgnore]
        public int RetainerCityId => DbCleaning.ReadCityId(RetainerCityIdInternal);

        [BsonElement("sellerID")]
        public object SellerIdInternal { get; init; }

        [BsonIgnore]
        public string SellerId => DbCleaning.NullIfZeroString(DbCleaning.ReadAsString(SellerIdInternal));

        [BsonElement("sourceName")]
        public string UploadApplicationName { get; init; }

        public bool Equals(Listing other)
        {
            // The upload application is not included in the equality check
            // because it's metadata specific to Universalis, not the game.
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ListingId == other.ListingId
                   && Hq == other.Hq
                   && OnMannequin == other.OnMannequin
                   && Materia.SequenceEqual(other.Materia)
                   && PricePerUnit == other.PricePerUnit
                   && Quantity == other.Quantity
                   && DyeId == other.DyeId
                   && CreatorId == other.CreatorId
                   && CreatorName == other.CreatorName
                   && Math.Abs(LastReviewTimeUnixSeconds - other.LastReviewTimeUnixSeconds) < 0.01
                   && RetainerId == other.RetainerId && RetainerName == other.RetainerName
                   && RetainerCityId == other.RetainerCityId
                   && SellerId == other.SellerId;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Listing)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(ListingId);
            hashCode.Add(Hq);
            hashCode.Add(OnMannequin);
            hashCode.Add(Materia);
            hashCode.Add(PricePerUnit);
            hashCode.Add(Quantity);
            hashCode.Add(DyeId);
            hashCode.Add(CreatorId);
            hashCode.Add(CreatorName);
            hashCode.Add(LastReviewTimeUnixSeconds);
            hashCode.Add(RetainerId);
            hashCode.Add(RetainerName);
            hashCode.Add(RetainerCityId);
            hashCode.Add(SellerId);
            return hashCode.ToHashCode();
        }
    }
}