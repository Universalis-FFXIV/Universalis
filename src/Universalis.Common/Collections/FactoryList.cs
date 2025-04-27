namespace Universalis.Common.Collections;

public static class FactoryList
{
    public static FactoryList<T> OfLength<T>(int length, Func<int, T> factory)
    {
        var list = new List<T>();
        for (var i = 0; i < length; i++)
        {
            list.Add(factory(i));
        }

        return new FactoryList<T>(list);
    }
}