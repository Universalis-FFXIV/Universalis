namespace Universalis.Mogboard.Doctrine.Parsers;

internal static class IntParser
{
    public static int Parse(ReadOnlySpan<char> buf)
    {
        var intPart = buf[2..];
        return int.Parse(intPart[^1] == ';' ? intPart[..^1] : intPart);
    }
}