using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class MostRecentlyUpdated
    {
        [BsonElement("wId")]
        public uint WorldId { get; init; }

        [BsonElement("u")]
        public List<WorldItemUpload> Uploads { get; set; }
    }
}