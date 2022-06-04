using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class UploadCountHistory
{
    [BsonElement("lastPush")]
    public double LastPush { get; set; }

    [BsonElement("uploadCountByDay")]
    public List<double> UploadCountByDay { get; set; }
}