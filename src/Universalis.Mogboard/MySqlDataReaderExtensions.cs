using FastMember;
using MySqlConnector;

namespace Universalis.Mogboard;

internal static class MySqlDataReaderExtensions
{
    // https://stackoverflow.com/a/44853182/14226597
    public static T ConvertToObject<T>(this MySqlDataReader rd) where T : class, new()
    {
        var type = typeof(T);
        var accessor = TypeAccessor.Create(type);
        var members = accessor.GetMembers();
        var t = new T();

        for (var i = 0; i < rd.FieldCount; i++)
        {
            if (!rd.IsDBNull(i))
            {
                string fieldName = rd.GetName(i);

                if (members.Any(m => string.Equals(m.Name, fieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    accessor[t, fieldName] = rd.GetValue(i);
                }
            }
        }

        return t;
    }
}