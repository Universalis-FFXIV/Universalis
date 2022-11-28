using Amazon.DynamoDBv2.DataModel;

namespace Universalis.Entities.Uploads;

[DynamoDBTable("flagged_uploader")]
public class FlaggedUploader
{
    [DynamoDBHashKey("id_sha256")]
    public string IdSha256 { get; init; }

    public FlaggedUploader()
    {
    }

    public FlaggedUploader(string idSha256)
    {
        IdSha256 = idSha256;
    }
}