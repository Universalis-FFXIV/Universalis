using System;
using System.Collections.Generic;

namespace Universalis.GameData;

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

    public IReadOnlySet<int> AvailableWorldIds() => _gdp.AvailableWorldIds();

    public IReadOnlyDictionary<int, string> AvailableWorlds() => _gdp.AvailableWorlds();

    public IReadOnlyDictionary<string, int> AvailableWorldsReversed() => _gdp.AvailableWorldsReversed();

    public IEnumerable<DataCenter> DataCenters() => _gdp.DataCenters();

    public IReadOnlySet<int> MarketableItemIds() => _gdp.MarketableItemIds();
    
    public IReadOnlyDictionary<int, int> MarketableItemStackSizes() => _gdp.MarketableItemStackSizes();
}