using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public class ApiKeyStore : IApiKeyStore
{
    private readonly IAmazonDynamoDB _dynamoDb;

    public ApiKeyStore(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        if (apiKey == null)
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        var context = new DynamoDBContext(_dynamoDb);
        await context.SaveAsync(apiKey, cancellationToken);
    }

    public Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default)
    {
        if (tokenSha512 == null)
        {
            throw new ArgumentNullException(nameof(tokenSha512));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.LoadAsync<ApiKey>(tokenSha512, cancellationToken);
    }
}