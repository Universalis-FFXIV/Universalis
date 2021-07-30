using System.Collections.Generic;
using Universalis.GameData;

namespace Universalis.Application.Tests.Mocks.GameData
{
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
            return new SortedSet<uint>(new uint[] { 5333 });
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
}