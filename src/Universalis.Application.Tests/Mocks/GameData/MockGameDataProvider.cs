using System.Collections.Generic;
using System.Linq;
using Universalis.GameData;

namespace Universalis.Application.Tests.Mocks.GameData;

public class MockGameDataProvider : IGameDataProvider
{
    public IReadOnlyDictionary<int, string> AvailableWorlds()
    {
        return new Dictionary<int, string>
        {
            { 74, "Coeurl" },
            { 34, "Brynhildr" },
        };
    }

    public IReadOnlyDictionary<string, int> AvailableWorldsReversed()
    {
        return new Dictionary<string, int>
        {
            { "Coeurl", 74 },
            { "Brynhildr", 34 },
        };
    }

    public IReadOnlySet<int> AvailableWorldIds()
    {
        return new SortedSet<int>(new int[] { 74, 34 });
    }

    public IReadOnlySet<int> MarketableItemIds()
    {
        return new SortedSet<int>(Enumerable.Range(1, 35000));
    }

    public IReadOnlyDictionary<int, int> MarketableItemStackSizes()
    {
        return new Dictionary<int, int>
        {
            { 5333, 99 },
            { 5, 9999 },
        };
    }

    public IEnumerable<DataCenter> DataCenters()
    {
        return new[]
        {
            new DataCenter
            {
                Name = "Crystal",
                Region = "North-America",
                WorldIds = new int[] { 74, 34 },
            },
        };
    }
}