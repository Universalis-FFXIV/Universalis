using System.Collections.Generic;
using System.Linq;
using Universalis.GameData;

namespace Universalis.Application.Tests.Mocks.GameData;

public class MockGameDataProvider : IGameDataProvider
{
    public IReadOnlyDictionary<uint, string> AvailableWorlds()
    {
        return new Dictionary<uint, string>
        {
            {74, "Coeurl"},
            {34, "Brynhildr"},
        };
    }

    public IReadOnlyDictionary<string, uint> AvailableWorldsReversed()
    {
        return new Dictionary<string, uint>
        {
            {"Coeurl", 74},
            {"Brynhildr", 34},
        };
    }

    public IReadOnlySet<uint> AvailableWorldIds()
    {
        return new SortedSet<uint>(new uint[] { 74, 34 });
    }

    public IReadOnlySet<uint> MarketableItemIds()
    {
        return new SortedSet<uint>(Enumerable.Range(1, 35000).Select(n => (uint)n));
    }
    
    public IReadOnlyDictionary<uint, uint> MarketableItemStackSizes()
    {
        return new Dictionary<uint, uint>
        {
            {5333, 99},
            {5, 9999},
        };
    }

    public IEnumerable<DataCenter> DataCenters()
    {
        return new[]
        {
            new DataCenter
            {
                Name = "Crystal",
                WorldIds = new uint[] {74, 34},
            }
        };
    }
}