using System;
using System.Globalization;
using System.Text;

namespace Universalis.Application.Views.V3.Market;

public readonly struct SalesCursor
{
    public readonly DateTime From;

    private SalesCursor(DateTime from)
    {
        From = from;
    }
    
    public override string ToString()
    {
        var data = From.ToString(CultureInfo.InvariantCulture);
        var utf8 = Encoding.UTF8.GetBytes(data);
        return Convert.ToBase64String(utf8);
    }

    public static bool TryParse(string s, out SalesCursor cursor)
    {
        cursor = default;
        
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }
        
        var utf8 = Convert.FromBase64CharArray(s.ToCharArray(), 0, s.Length);
        var data = Encoding.UTF8.GetString(utf8);

        if (DateTime.TryParse(data, out var from))
        {
            cursor = new SalesCursor(from);
            return true;
        }

        return false;
    }

    public static SalesCursor FromDateTime(DateTime from)
    {
        return new SalesCursor(from);
    }
    
    public static SalesCursor FromUnixMilliseconds(long unix)
    {
        var offset = DateTimeOffset.FromUnixTimeMilliseconds(unix);
        var from = offset.UtcDateTime;
        return new SalesCursor(from);
    }

    public static SalesCursor Create()
    {
        return new SalesCursor(DateTime.UtcNow);
    }
}