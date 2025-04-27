using System.Collections;

namespace Universalis.Common.Collections;

public class FactoryList<T> : IList<T>
{
    private readonly List<T> _list;

    public int Count => _list.Count;
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    internal FactoryList(List<T> list)
    {
        _list = list;
    }

    public void ForEach(Action<T> action) => _list.ForEach(action);

    public void ForEachParallel(int parallelism, Action<T> action, CancellationToken cancellationToken = default) =>
        Parallel.ForEach(_list, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = parallelism,
        }, action);

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Add(T item) => _list.Add(item);
    public void Clear() => _list.Clear();
    public bool Contains(T item) => _list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
    public bool Remove(T item) => _list.Remove(item);
    public int IndexOf(T item) => _list.IndexOf(item);
    public void Insert(int index, T item) => _list.Insert(index, item);
    public void RemoveAt(int index) => _list.RemoveAt(index);
}