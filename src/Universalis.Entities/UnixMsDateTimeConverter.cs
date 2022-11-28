using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;

namespace Universalis.Entities;

public class UnixMsDateTimeConverter : IPropertyConverter
{
    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is not Primitive primitive ||
            primitive.Value is not string ||
            !long.TryParse((string)primitive.Value, out var timestamp))
        {
            throw new ArgumentOutOfRangeException(nameof(entry));
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
    }

    public DynamoDBEntry ToEntry(object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new Primitive
        {
            Type = DynamoDBEntryType.Numeric,
            Value = new DateTimeOffset((DateTime)value).ToUnixTimeMilliseconds().ToString(),
        };
    }
}
