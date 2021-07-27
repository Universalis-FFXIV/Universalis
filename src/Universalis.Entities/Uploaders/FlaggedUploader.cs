using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploaders
{
    public class FlaggedUploader
    {
        [BsonElement("uploaderID")]
        public string UploaderId { get; set; }
    }
}