using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Queries.Uploads;

namespace Universalis.Application;

// TODO: Determine if this can be removed; likely a premature optimization
public static class TrustedSourceHashCache
{
    private static readonly ConcurrentDictionary<string, string> Hashes = new();

    public static async Task<string> GetHash(string apiKey, ITrustedSourceDbAccess dbAccess, CancellationToken cancellationToken = default)
    {
        if (Hashes.TryGetValue(apiKey, out var hashString))
        {
            // Return cached result
            return hashString;
        }

        using var sha512 = SHA512.Create();
        hashString = Util.Hash(sha512, apiKey);

        var source = await dbAccess.Retrieve(new TrustedSourceQuery { ApiKeySha512 = hashString }, cancellationToken);
        if (source == null)
        {
            // Return invalid hash string without caching the result
            return hashString;
        }

        // Cache and return result
        Hashes[apiKey] = hashString;
        return hashString;
    }
}