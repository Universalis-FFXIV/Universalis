namespace Universalis.Mogboard.Doctrine.Serializers;

public static class ValueSerializer
{
    public static string Serialize(object o)
    {
        return o switch
        {
            string s => StringSerializer.Serialize(s),
            int n => IntSerializer.Serialize(n),
            IList<object> a => ArraySerializer.Serialize(a),
            _ => throw new ArgumentException("Received object with no known serializer.", nameof(o)),
        };
    }
}