using System;

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
}