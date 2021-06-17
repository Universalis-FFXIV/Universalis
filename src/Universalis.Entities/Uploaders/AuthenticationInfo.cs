namespace Universalis.Entities.Uploaders
{
    public class AuthenticationInfo
    {
        public byte UploadApplication { get; set; }

        public string ApiKeySha256 { get; set; } // There's no real reason for this to be hashed, but ~legacy~
    }
}