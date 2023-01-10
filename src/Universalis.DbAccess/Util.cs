using System;
using System.Security.Cryptography;
using System.Text;

namespace Universalis.DbAccess;

public static class Util
{
    /// <summary>
    /// Hashes the provided string.
    /// </summary>
    /// <param name="hasher">The hashing algorithm to use.</param>
    /// <param name="input">The input string.</param>
    /// <returns>A hash representing the input string.</returns>
    public static string Hash(HashAlgorithm hasher, string input)
    {
        Span<byte> hash = stackalloc byte[hasher.HashSize/8];
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input ?? "");
        if (hasher.TryComputeHash(bytes, hash, out var _written)) // Since we stackalloc the hash buffer, written is not needed
            return Convert.ToHexString(hash).ToLowerInvariant(); // https://github.com/dotnet/runtime/issues/60393
        throw new InvalidOperationException("Destination buffer was too small, this should never occur");
    }
}