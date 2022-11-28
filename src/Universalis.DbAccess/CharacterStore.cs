using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Universalis.Entities;

namespace Universalis.DbAccess;

public class CharacterStore : ICharacterStore
{
    private readonly IAmazonDynamoDB _dynamoDb;

    public CharacterStore(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public Task Insert(Character character, CancellationToken cancellationToken = default)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.SaveAsync(character, cancellationToken);
    }
    
    public Task Update(Character character, CancellationToken cancellationToken = default)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.SaveAsync(character, cancellationToken);
    }

    public Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        if (contentIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(contentIdSha256));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.LoadAsync<Character>(contentIdSha256, cancellationToken);
    }
}
