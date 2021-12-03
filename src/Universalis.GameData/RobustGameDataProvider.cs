using System;

namespace Universalis.GameData
{
    public class RobustGameDataProvider : IGameDataProvider
    {
        private readonly IGameDataProvider _gdp;

        public RobustGameDataProvider(RobustGameDataProviderParams opts)
        {
            try
            {
                _gdp = new LuminaGameDataProvider(opts.SqPack);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to start Lumina provider: {0}", e);
                _gdp = new CsvGameDataProvider(opts.Http);
            }
        }

        public IReadOnlySet<uint> AvailableWorldIds() => _gdp.AvailableWorldIds();

        public IReadOnlyDictionary<uint, string> AvailableWorlds() => _gdp.AvailableWorlds();

        public IReadOnlyDictionary<string, uint> AvailableWorldsReversed() => _gdp.AvailableWorldsReversed();

        public IEnumerable<DataCenter> DataCenters() => _gdp.DataCenters();

        public IReadOnlySet<uint> MarketableItemIds() => _gdp.MarketableItemIds();
    }
}