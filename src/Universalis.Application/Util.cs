using System.Text.RegularExpressions;

namespace Universalis.Application
{
    public static class Util
    {
        private static readonly Regex HtmlTags = new(@"<[\s\S]*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex UnsafeCharacters =
            new(@"[^a-zA-Z0-9'\- ·⺀-⺙⺛-⻳⼀-⿕々〇〡-〩〸-〺〻㐀-䶵一-鿃豈-鶴侮-頻並-龎]", RegexOptions.Compiled);

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
        /// </summary>
        /// <param name="b">The input text.</param>
        /// <returns>A boolean corresponding to the text.</returns>
        public static bool ParseUnusualBool(string b)
        {
            b = b?.ToLowerInvariant();
            return b switch
            {
                "true" or "1" => true,
                "false" or "0" => false,
                _ => false,
            };
        }
    }
}