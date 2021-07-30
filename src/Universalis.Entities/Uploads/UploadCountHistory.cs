using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class UploadCountHistory : ExtraData
    {
        public static readonly string DefaultSetName = "uploadCountHistory";

        [BsonElement("lastPush")]
        public uint LastPush { get; set; }

        [BsonElement("uploadCountByDay")]
        public List<uint> UploadCountByDay { get; set; }

        public UploadCountHistory() : base(DefaultSetName) { }
    }
}