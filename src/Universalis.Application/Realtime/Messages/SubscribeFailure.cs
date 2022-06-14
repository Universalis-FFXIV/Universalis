using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public class SubscribeFailure : SocketMessage
{
    [BsonElement("reason")]
    public string Reason { get; }
    
    public SubscribeFailure(string reason) : base("subscribe", "error")
    {
        Reason = reason;
    }
}