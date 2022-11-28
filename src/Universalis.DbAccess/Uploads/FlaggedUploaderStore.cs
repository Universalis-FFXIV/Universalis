using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class FlaggedUploaderStore : IFlaggedUploaderStore
{
    private readonly IAmazonDynamoDB _dynamoDb;

    public FlaggedUploaderStore(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public Task Insert(FlaggedUploader uploader, CancellationToken cancellationToken = default)
    {
        if (uploader == null)
        {
            throw new ArgumentNullException(nameof(uploader));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.SaveAsync(uploader, cancellationToken);
    }

    public Task<FlaggedUploader> Retrieve(string uploaderIdSha256, CancellationToken cancellationToken = default)
    {
        if (uploaderIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(uploaderIdSha256));
        }

        var context = new DynamoDBContext(_dynamoDb);
        return context.LoadAsync<FlaggedUploader>(uploaderIdSha256, cancellationToken);
    }
}