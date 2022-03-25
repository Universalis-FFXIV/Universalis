namespace Universalis.Mogboard.Doctrine.Serializers;

internal static class StringSerializer
{
    public static string Serialize(string s)
    {
        return $"s:{s.Length}:\"{s}\"";
    }
}