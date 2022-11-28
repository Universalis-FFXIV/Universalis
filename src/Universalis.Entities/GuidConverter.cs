using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;

namespace Universalis.Entities;

public class GuidConverter : IPropertyConverter
{
    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is not Primitive primitive ||
            primitive.Value is not string ||
            !Guid.TryParse((string)primitive.Value, out var guid))
        {
            throw new ArgumentOutOfRangeException(nameof(entry));
        }

        return guid;
    }

    public DynamoDBEntry ToEntry(object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new Primitive
        {
            Value = ((Guid)value).ToString(),
        };
    }
}
