namespace Universalis.Mogboard.Doctrine.Parsers;

internal static class ValueParser
{
    public static object Parse(string s)
    {
        ReadOnlySpan<char> buf = s;
        return Parse(buf);
    }

    public static object Parse(ReadOnlySpan<char> buf)
    {
        // Serialized Doctrine values have a structure of "x:y", where "x" is a identifier for the value type,
        // and "y" is the value itself. "y" can also have a more complicated structure, but we won't worry too
        // much about that right now.

        // In the simplest case, a serialized value must have at least 3 characters.
        if (buf.Length < 3)
        {
            throw new ArgumentException("Input format is invalid.", nameof(buf));
        }

        return buf[0] switch
        {
            'a' => ArrayParser.Parse(buf),
            'i' => IntParser.Parse(buf),
            _ => throw new InvalidOperationException($"Unknown type specifier \"{buf[0]}\"."),
        };
    }
}