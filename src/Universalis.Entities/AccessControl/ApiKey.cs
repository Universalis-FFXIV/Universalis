using System;
using System.Security.Cryptography;
using System.Text;

namespace Universalis.Entities.AccessControl;

public class ApiKey
{
    public string TokenSha512 { get; init; }

    public string Name { get; init; }

    public bool CanUpload { get; init; }

    public ApiKey()
    {
    }

    public ApiKey(string tokenSha512, string name, bool canUpload)
    {
        if (string.IsNullOrEmpty(tokenSha512))
        {
            throw new ArgumentNullException(nameof(tokenSha512));
        }
        
        TokenSha512 = tokenSha512;
        Name = name;
        CanUpload = canUpload;
    }

    public static ApiKey FromToken(string token, string name, bool canUpload)
    {
        using var sha512 = SHA512.Create();
        var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(token));
        var hashStr = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return new ApiKey(hashStr, name, canUpload);
    }
}