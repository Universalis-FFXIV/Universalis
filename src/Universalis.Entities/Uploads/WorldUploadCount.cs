using System.Threading;

namespace Universalis.Entities.Uploads;

public class WorldUploadCount
{
    public long Count
    {
        get => _count;
        init => _count = value;
    }

    public string WorldName { get; init; }

    private long _count;

    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }
}