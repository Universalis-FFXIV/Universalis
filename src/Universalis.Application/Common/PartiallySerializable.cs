using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Common; 

public abstract class PartiallySerializable {
    
    [JsonIgnore] 
    public HashSet<string> SerializableProperties { get; set; }
}
