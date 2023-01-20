using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Universalis.GameData;

public class DynamicGameDataProvider : IGameDataProvider
{
    private readonly ILogger<DynamicGameDataProvider> _logger;
    private readonly IGameDataProvider _gdp;

    public DynamicGameDataProvider(DynamicGameDataProviderOptions opts, ILogger<DynamicGameDataProvider> logger)
    {
        _logger = logger;
        try
        {
            _gdp = LoadLumina(opts);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load Lumina");
            _gdp = LoadCsv(opts);
        }
    }

    private IGameDataProvider LoadCsv(DynamicGameDataProviderOptions opts)
    {
        _logger.LogInformation("Starting CSV game data provider");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var gdp = new CsvGameDataProvider(opts.Http);
        stopwatch.Stop();
        _logger.LogInformation("CSV game data provider successfully loaded in {}", stopwatch.Elapsed);
        return gdp;
    }

    private IGameDataProvider LoadLumina(DynamicGameDataProviderOptions opts)
    {
        _logger.LogInformation("Starting Lumina on path {}", opts.SqPack);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var gdp = new LuminaGameDataProvider(opts.SqPack);
        stopwatch.Stop();
        _logger.LogInformation("Lumina successfully loaded in {}", stopwatch.Elapsed);
        return gdp;
    }

    public IReadOnlySet<int> AvailableWorldIds() => _gdp.AvailableWorldIds();

    public IReadOnlyDictionary<int, string> AvailableWorlds() => _gdp.AvailableWorlds();

    public IReadOnlyDictionary<string, int> AvailableWorldsReversed() => _gdp.AvailableWorldsReversed();

    public IEnumerable<DataCenter> DataCenters() => _gdp.DataCenters();

    public IReadOnlySet<int> MarketableItemIds() => _gdp.MarketableItemIds();

    public IReadOnlyDictionary<int, int> MarketableItemStackSizes() => _gdp.MarketableItemStackSizes();
}