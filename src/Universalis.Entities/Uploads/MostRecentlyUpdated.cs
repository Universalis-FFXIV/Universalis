using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class MostRecentlyUpdated
    {
        [BsonElement("worldId")]
        public uint WorldId { get; init; }

        [BsonElement("uploads")]
        public List<WorldItemUpload> Uploads { get; set; }
    }
}