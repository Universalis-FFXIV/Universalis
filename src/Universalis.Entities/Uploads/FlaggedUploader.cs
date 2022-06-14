namespace Universalis.Entities.Uploads;

public class FlaggedUploader
{
    public string IdSha256 { get; }

    public FlaggedUploader(string idSha256)
    {
        IdSha256 = idSha256;
    }
}