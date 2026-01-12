using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Tests.Mocks.Realtime.Messages;

public class MockMessage : SocketMessage
{
    [BsonElement("value")]
    public int Value { get; init; }

    [BsonIgnore]
    private Dictionary<string, string>? _filterValues;

    public MockMessage(params string[] channels) : base(channels)
    {
    }

    public override IReadOnlyDictionary<string, string> GetFilterValues()
    {
        return _filterValues ??= new Dictionary<string, string>
        {
            ["value"] = Value.ToString(),
        };
    }
}