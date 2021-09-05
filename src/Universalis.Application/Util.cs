using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;

namespace Universalis.Application
{
    public static class Util
    {
        private static readonly Regex HtmlTags = new(@"<[\s\S]*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex UnsafeCharacters =
            new(@"[^a-zA-Z0-9'\- ·⺀-⺙⺛-⻳⼀-⿕々〇〡-〩〸-〺〻㐀-䶵一-鿃豈-鶴侮-頻並-龎]", RegexOptions.Compiled);

        private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

        public static async Task<string> Hash(HashAlgorithm hasher, string id, CancellationToken cancellationToken = default)
        {
            var idBytes = Encoding.UTF8.GetBytes(id ?? "");
            await using var dataStream = MemoryStreamPool.GetStream(idBytes);
            return Util.BytesToString(await hasher.ComputeHashAsync(dataStream, cancellationToken));
        }

        public static string BytesToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Returns <see langword="true" /> if the provided input contains HTML tags.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns><see langword="true" /> if the input contains HTML tags, otherwise <see langword="false" />.</returns>
        public static bool HasHtmlTags(string input)
        {
            return HtmlTags.IsMatch(input);
        }

        /// <summary>
        /// Removes unsafe characters from the input text.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>The input text with any unsafe characters removed.</returns>
        public static string RemoveUnsafeCharacters(string input)
        {
            return UnsafeCharacters.Replace(input, "");
        }

        /// <summary>
        /// Parses a bool that is provided as a string or a number into a proper boolean value.
        /// This function exists because of unfortunate inconsistencies in how different
        /// clients upload parsed values.
        /// </summary>
        /// <param name="o">The input object.</param>
        /// <returns>A boolean corresponding to the text.</returns>
        public static bool ParseUnusualBool(object o)
        {
            // Conversions for System.Text.Json types
            o = o switch
            {
                JsonElement { ValueKind: JsonValueKind.Number } e => e.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),
                JsonElement { ValueKind: JsonValueKind.True } => true,
                JsonElement { ValueKind: JsonValueKind.False } => false,
                _ => o,
            };

            if (o is bool b)
            {
                return b;
            }

            if (o is not string s)
            {
                return false;
            }

            s = s.ToLowerInvariant();
            return s switch
            {
                "true" or "1" => true,
                "false" or "0" => false,
                _ => false,
            };
        }

        /// <summary>
        /// Parses an ID that provided as a full ID or one with 0 as a sentinel for null into a null-if-absent string.
        /// </summary>
        /// <param name="id">The ID to parse.</param>
        /// <returns>A string corresponding to the meaning of the ID.</returns>
        public static string ParseUnusualId(object id)
        {
            return id switch
            {
                JsonElement { ValueKind: JsonValueKind.Number } e => e.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),

                "0" => null,
                string s => s,
                _ => null,
            };
        }
    }
}