namespace Universalis.Mogboard.Doctrine.Parsers;

internal static class StringParser
{
    public static string Parse(ReadOnlySpan<char> buf)
    {
        var bufPart1 = buf[2..];
        var bufPart2 = bufPart1[(bufPart1.IndexOf(':') + 2)..];
        return new string(bufPart2[..^1]);
    }
}