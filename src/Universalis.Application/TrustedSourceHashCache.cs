using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application
{
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
            await using var authStream = new MemoryStream(Encoding.UTF8.GetBytes(apiKey));
            var hash = await sha512.ComputeHashAsync(authStream, cancellationToken);
            hashString = Util.BytesToString(hash);

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
}