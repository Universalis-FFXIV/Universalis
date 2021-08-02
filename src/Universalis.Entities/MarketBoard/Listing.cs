using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Universalis.Entities.MarketBoard
{
    public class Listing : IEquatable<Listing>
    {
        [BsonElement("listingID")]
        public string ListingId { get; init; }

        [BsonElement("hq")]
        public bool Hq { get; init; }

        [BsonElement("onMannequin")]
        public bool OnMannequin { get; init; }

        [BsonElement("materia")]
        public List<Materia> Materia { get; init; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; init; }

        [BsonElement("quantity")]
        public uint Quantity { get; init; }

        [BsonElement("stainID")]
        public uint DyeId { get; init; }

        [BsonElement("creatorID")]
        public string CreatorIdHash { get; init; }

        [BsonElement("creatorName")]
        public string CreatorName { get; init; }

        [BsonElement("lastReviewTime")]
        public double LastReviewTimeUnixSeconds { get; init; }

        [BsonElement("retainerID")]
        public string RetainerId { get; init; }

        [BsonElement("retainerName")]
        public string RetainerName { get; init; }

        [BsonElement("retainerCity")]
        public BsonBinaryData RetainerCityIdInternal { get; init; }

        [BsonIgnore]
        public byte RetainerCityId =>
            RetainerCityIdInternal.IsNumeric
                ? (byte)RetainerCityIdInternal.AsInt32
                : City.Dict[RetainerCityIdInternal.AsString];

        [BsonElement("sellerID")]
        public string SellerIdHash { get; init; }

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
                   && CreatorIdHash == other.CreatorIdHash
                   && CreatorName == other.CreatorName
                   && LastReviewTimeUnixSeconds == other.LastReviewTimeUnixSeconds
                   && RetainerId == other.RetainerId && RetainerName == other.RetainerName
                   && RetainerCityId == other.RetainerCityId
                   && SellerIdHash == other.SellerIdHash;
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
            hashCode.Add(CreatorIdHash);
            hashCode.Add(CreatorName);
            hashCode.Add(LastReviewTimeUnixSeconds);
            hashCode.Add(RetainerId);
            hashCode.Add(RetainerName);
            hashCode.Add(RetainerCityId);
            hashCode.Add(SellerIdHash);
            return hashCode.ToHashCode();
        }
    }
}