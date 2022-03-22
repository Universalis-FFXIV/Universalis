namespace Universalis.Mogboard.Doctrine.Parsers;

internal static class ArrayParser
{
    public static object[] Parse(ReadOnlySpan<char> buf)
    {
        var ptr = 2;
        
        var lengthEnd = ptr;
        for (; lengthEnd < buf.Length; lengthEnd++)
        {
            if (buf[lengthEnd] == ':')
            {
                break;
            }
        }

        if (lengthEnd == buf.Length)
        {
            throw new ArgumentException("No array data.", nameof(buf));
        }

        var length = int.Parse(buf[ptr..lengthEnd]);
        ptr = lengthEnd + 1;

        var arr = new object[length];

        return ParseStart(arr, buf[ptr..]);
    }

    private static object[] ParseStart(object[] arr, ReadOnlySpan<char> remainder)
    {
        return ParseIndex(arr, remainder);
    }

    private static object[] ParseIndex(object[] arr, ReadOnlySpan<char> remainder)
    {
        // Start of array
        if (remainder[0] == '{')
        {
            return ParseIndex(arr, remainder[1..]);
        }

        // End of array
        if (remainder[0] == '}')
        {
            return arr;
        }

        // Parse next index
        var ptr = 0;
        var endPtr = ptr;
        for (; endPtr < remainder.Length; endPtr++)
        {
            if (remainder[endPtr] == ';')
            {
                break;
            }
        }

        if (endPtr == remainder.Length)
        {
            throw new ArgumentException("No more array data.", nameof(remainder));
        }
        
        var idx = (int)ValueParser.Parse(remainder[ptr..endPtr]);
        ptr = endPtr + 1;

        // After parsing an index, we want to parse its associated value
        return Parse(arr, idx, remainder[ptr..]);
    }

    private static object[] Parse(object[] arr, int idx, ReadOnlySpan<char> remainder)
    {
        // Start of array
        if (remainder[0] == '{')
        {
            throw new ArgumentException("Got start of array; was ParseIndex called first?", nameof(remainder));
        }

        // End of array
        if (remainder[0] == '}')
        {
            throw new ArgumentException("Got end of array while parsing array value.", nameof(remainder));
        }

        // Parse next value
        var ptr = 0;
        var endPtr = ptr;
        for (; endPtr < remainder.Length; endPtr++)
        {
            if (remainder[endPtr] == ';')
            {
                break;
            }
        }

        if (endPtr == remainder.Length)
        {
            throw new ArgumentException("No more array data.", nameof(remainder));
        }

        arr[idx] = ValueParser.Parse(remainder[ptr..endPtr]);
        ptr = endPtr + 1;

        // After parsing a value, we want to parse the next index
        return ParseIndex(arr, remainder[ptr..]);
    }
}