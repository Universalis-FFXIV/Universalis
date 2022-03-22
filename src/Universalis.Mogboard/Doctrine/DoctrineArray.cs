using System.Collections;
using Universalis.Mogboard.Doctrine.Parsers;
using Universalis.Mogboard.Doctrine.Serializers;

namespace Universalis.Mogboard.Doctrine;

public class DoctrineArray<T> : IEnumerable<T>
{
    private T[]? _array;

    public int Length => _array!.Length;

    public T this[int idx]
    {
        get => _array![idx];
        set => _array![idx] = value;
    }

    private DoctrineArray() { }

    public DoctrineArray(int size)
    {
        _array = new T[size];
    }

    public DoctrineArray(T[] values)
    {
        _array = values ?? throw new ArgumentException("Array must not be null.", nameof(values));
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return ArraySerializer.Serialize(_array!.Cast<object>().ToList());
    }

    public static DoctrineArray<T> Parse(string s)
    {
        var arr = ArrayParser.Parse(s);
        var typedArr = new DoctrineArray<T>
        {
            _array = arr.Cast<T>().ToArray(),
        };

        return typedArr;
    }
}