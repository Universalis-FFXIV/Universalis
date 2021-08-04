using System;
using System.Globalization;

namespace Universalis.Entities
{
    internal static class DbCleaning
    {
        public static string ReadAsString(object o)
        {
            return o switch
            {
                string s => s,
                null => null,
                double d => Math.Truncate(d).ToString(CultureInfo.InvariantCulture),
                _ => o.ToString(),
            };
        }

        public static int ReadCityId(object o)
        {
            return o switch
            {
                int i => i,
                null => 0,
                _ => City.Dict[(string)o],
            };
        }
    }
}