using System;
using System.Diagnostics;
using System.Reflection;
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
        if (hasher.TryComputeHash(bytes, hash, out _)) // Since we stackalloc the hash buffer, written is not needed
            return Convert.ToHexString(hash).ToLowerInvariant(); // https://github.com/dotnet/runtime/issues/60393
        throw new InvalidOperationException("Destination buffer was too small, this should never occur");
    }

    internal static readonly AssemblyName Assembly
        = typeof(Util).Assembly.GetName();

    public static readonly ActivitySource ActivitySource
        = new(Assembly.Name ?? "Universalis.DbAccess", Assembly.Version?.ToString() ?? "0.0");
}