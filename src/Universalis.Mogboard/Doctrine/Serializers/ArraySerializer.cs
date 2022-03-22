using System.Text;

namespace Universalis.Mogboard.Doctrine.Serializers;

internal static class ArraySerializer
{
    public static string Serialize(IList<object> arr)
    {
        var capacityEstimate = arr.Count * 2;
        var sb = new StringBuilder(capacityEstimate);
        sb.Append("a:");
        sb.Append(arr.Count);
        sb.Append(":{");
        for (var i = 0; i < arr.Count; i++)
        {
            sb.Append("i:");
            sb.Append(i);
            sb.Append(';');
            var val = ValueSerializer.Serialize(arr[i]);
            sb.Append(val);
            sb.Append(';');
        }
        sb.Append('}');
        return sb.ToString();
    }
}