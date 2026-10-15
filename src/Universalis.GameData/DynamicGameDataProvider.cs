using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Universalis.GameData;

public class DynamicGameDataProvider : IGameDataProvider
{
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger<DynamicGameDataProvider> _logger;
    private readonly DynamicGameDataProviderOptions _opts;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private volatile IGameDataProvider _gdp;
    private DateTimeOffset _lastFailure;
    private Exception _lastError;

    public DynamicGameDataProvider(DynamicGameDataProviderOptions opts, ILogger<DynamicGameDataProvider> logger)
    {
        _opts = opts;
        _logger = logger;
    }

    private IGameDataProvider Gdp => Get();

    public IReadOnlySet<int> AvailableWorldIds() => Gdp.AvailableWorldIds();

    public IReadOnlyDictionary<int, string> AvailableWorlds() => Gdp.AvailableWorlds();

    public IReadOnlyDictionary<string, int> AvailableWorldsReversed() => Gdp.AvailableWorldsReversed();

    public IEnumerable<DataCenter> DataCenters() => Gdp.DataCenters();

    public IReadOnlySet<int> MarketableItemIds() => Gdp.MarketableItemIds();

    public IReadOnlyDictionary<int, int> MarketableItemStackSizes() => Gdp.MarketableItemStackSizes();

    private IGameDataProvider Get()
    {
        // Fast path on prior initialization success
        var gdp = _gdp;
        if (gdp != null) return gdp;

        _initLock.Wait();
        try
        {
            if (_gdp != null) return _gdp;

            // Fail fast between retries to avoid a request stampede
            var timeSinceFailure = DateTimeOffset.UtcNow - _lastFailure;
            if (_lastError != null && timeSinceFailure < MinRetryInterval)
            {
                throw new InvalidOperationException(
                    $"Game data initialization failed {(int)timeSinceFailure.TotalSeconds}s ago; retrying after a {(int)MinRetryInterval.TotalSeconds}s backoff. Root cause follows.",
                    _lastError);
            }

            try
            {
                _gdp = TryLoad();
                _lastError = null;
                _lastFailure = default;
                return _gdp;
            }
            catch (Exception e)
            {
                _lastFailure = DateTimeOffset.UtcNow;
                _lastError = e;
                throw new InvalidOperationException(
                    "All game data providers failed to initialize; retrying after backoff. See inner exception and logs for per-provider failures.", e);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private IGameDataProvider TryLoad()
    {
        try
        {
            return LoadLumina(_opts);
        }
        catch (Exception e1)
        {
            _logger.LogError(e1, "Failed to load Lumina");
        }

        try
        {
            var gdp = LoadBoilmaster(_opts);
            _logger.LogWarning("Serving game data from the Boilmaster fallback because Lumina failed to initialize");
            return gdp;
        }
        catch (Exception e2)
        {
            _logger.LogError(e2, "Failed to load Boilmaster game data");
        }

        var csv = LoadCsv(_opts);
        _logger.LogWarning("Serving game data from the CSV fallback because Lumina and Boilmaster failed to initialize");
        return csv;
    }

    private CsvGameDataProvider LoadCsv(DynamicGameDataProviderOptions opts)
    {
        _logger.LogInformation("Starting CSV game data provider");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var gdp = new CsvGameDataProvider(opts.Http);
        stopwatch.Stop();
        _logger.LogInformation("CSV game data provider successfully loaded in {}", stopwatch.Elapsed);
        return gdp;
    }

    private BoilmasterGameDataProvider LoadBoilmaster(DynamicGameDataProviderOptions opts)
    {
        _logger.LogInformation("Starting Boilmaster game data provider");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var gdp = new BoilmasterGameDataProvider(opts.Http, _logger);
        stopwatch.Stop();
        _logger.LogInformation("Boilmaster game data provider successfully loaded in {}", stopwatch.Elapsed);
        return gdp;
    }

    private LuminaGameDataProvider LoadLumina(DynamicGameDataProviderOptions opts)
    {
        _logger.LogInformation("Starting Lumina on path {}", opts.SqPack);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var gdp = new LuminaGameDataProvider(opts.SqPack);
        stopwatch.Stop();
        _logger.LogInformation("Lumina successfully loaded in {}", stopwatch.Elapsed);
        return gdp;
    }
}
