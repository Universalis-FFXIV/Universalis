using System;
using System.Security.Cryptography;
using System.Text;

namespace Universalis.Entities.AccessControl;

public class ApiKey
{
    public string TokenSha512 { get; }
    
    public string Name { get; }
    
    public bool CanUpload { get; }

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
        var hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return new ApiKey(hashStr, name, canUpload);
    }
}