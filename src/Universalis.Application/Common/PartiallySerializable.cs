using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Common; 

public abstract class PartiallySerializable {
    
    [BsonIgnore]
    [JsonIgnore] 
    public HashSet<string> SerializableProperties { get; set; }
}
