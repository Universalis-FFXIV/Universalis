namespace Universalis.Mogboard.Doctrine.Parsers;

internal static class IntParser
{
    public static int Parse(ReadOnlySpan<char> buf)
    {
        return int.Parse(buf[2..]);
    }
}