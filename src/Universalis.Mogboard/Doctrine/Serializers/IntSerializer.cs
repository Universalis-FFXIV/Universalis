namespace Universalis.Mogboard.Doctrine.Serializers;

internal static class IntSerializer
{
    public static string Serialize(int n)
    {
        return $"i:{n}";
    }
}