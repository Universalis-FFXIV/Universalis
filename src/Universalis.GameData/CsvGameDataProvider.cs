using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Universalis.GameData;

public class CsvGameDataProvider : IGameDataProvider
{
    private IReadOnlyDictionary<int, string> _availableWorlds;
    private IReadOnlyDictionary<string, int> _availableWorldsReversed;
    private IReadOnlySet<int> _availableWorldIds;

    private IReadOnlySet<int> _marketableItemIds;
    private IReadOnlyDictionary<int, int> _marketableItemStackSizes;

    private IReadOnlyList<DataCenter> _dataCenters;

    private readonly HttpClient _http;

    public CsvGameDataProvider(HttpClient http)
    {
        _http = http;

        // This is an anti-pattern but it only runs once so it's fine
        Task.Run(async () =>
        {
            var worlds = await GetWorlds();
            var items = await GetItems();
            var dcs = await GetDataCenters();

            _availableWorlds = await LoadAvailableWorlds(worlds);
            _availableWorldsReversed = await LoadAvailableWorldsReversed(worlds);
            _availableWorldIds = await LoadAvailableWorldIds(worlds);

            _marketableItemIds = await LoadMarketableItems(items);
            _marketableItemStackSizes = await LoadMarketableItemStackSizes(items);

            _dataCenters = await LoadDataCenters(worlds, dcs);
        }).GetAwaiter().GetResult();
    }

    IReadOnlyDictionary<int, string> IGameDataProvider.AvailableWorlds()
        => _availableWorlds;

    IReadOnlyDictionary<string, int> IGameDataProvider.AvailableWorldsReversed()
        => _availableWorldsReversed;

    IReadOnlySet<int> IGameDataProvider.AvailableWorldIds()
        => _availableWorldIds;

    IReadOnlySet<int> IGameDataProvider.MarketableItemIds()
        => _marketableItemIds;

    IReadOnlyDictionary<int, int> IGameDataProvider.MarketableItemStackSizes()
        => _marketableItemStackSizes;

    IEnumerable<DataCenter> IGameDataProvider.DataCenters()
        => _dataCenters;

    private async Task<IList<CsvWorld>> GetWorlds()
    {
        var csvData =
            await _http.GetStreamAsync(
                "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        for (var i = 0; i < 3; i++) await csv.ReadAsync();
        var worlds = csv.GetRecords<CsvWorld>().ToList();
        return worlds;
    }

    private async Task<IList<CsvItem>> GetItems()
    {
        var csvData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        await csv.ReadAsync();
        await csv.ReadAsync();
        csv.ReadHeader();
        await csv.ReadAsync();
        var items = csv.GetRecords<CsvItem>().ToList();
        return items;
    }

    private async Task<IList<CsvDc>> GetDataCenters()
    {
        var dcData =
            await _http.GetStreamAsync(
                "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/WorldDCGroupType.csv");
        using var dcReader = new StreamReader(dcData);
        using var dcCsv = new CsvReader(dcReader, CultureInfo.InvariantCulture);
        for (var i = 0; i < 3; i++) await dcCsv.ReadAsync();
        var dcs = dcCsv.GetRecords<CsvDc>().ToList();
        return dcs;
    }

    private static Task<IReadOnlyDictionary<int, string>> LoadAvailableWorlds(IEnumerable<CsvWorld> worlds)
    {
        return Task.FromResult<IReadOnlyDictionary<int, string>>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Id, w => w.Name));
    }

    private static Task<IReadOnlyDictionary<string, int>> LoadAvailableWorldsReversed(IEnumerable<CsvWorld> worlds)
    {
        return Task.FromResult<IReadOnlyDictionary<string, int>>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Name, w => w.Id));
    }

    private static Task<IReadOnlySet<int>> LoadAvailableWorldIds(IEnumerable<CsvWorld> worlds)
    {
        return Task.FromResult<IReadOnlySet<int>>(new SortedSet<int>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Select(w => w.Id)
            .ToList()));
    }

    private static Task<IReadOnlySet<int>> LoadMarketableItems(IEnumerable<CsvItem> items)
    {
        return Task.FromResult<IReadOnlySet<int>>(new SortedSet<int>(items
            .Where(i => i.ItemSearchCategory >= 1)
            .Select(i => i.RowId)
            .ToList()));
    }

    private static Task<IReadOnlyDictionary<int, int>> LoadMarketableItemStackSizes(IEnumerable<CsvItem> items)
    {
        return Task.FromResult<IReadOnlyDictionary<int, int>>(items
            .Where(i => i.ItemSearchCategory >= 1)
            .ToDictionary(i => i.RowId, i => i.StackSize));
    }

    private static Task<IReadOnlyList<DataCenter>> LoadDataCenters(IEnumerable<CsvWorld> worlds, IEnumerable<CsvDc> dcs)
    {
        return Task.FromResult<IReadOnlyList<DataCenter>>(dcs
            .Where(dc => dc.RowId is > 0 and < 99)
            .Select(dc => new DataCenter
            {
                Name = dc.Name,
                Region = Regions.Map[dc.Region],
                WorldIds = GetValidWorlds(worlds)
                    .Where(w => w.DataCenter == dc.RowId)
                    .Select(w => w.RowId)
                    .ToArray(),
            })
            .Where(dc => dc.WorldIds.Length > 0)
            .Concat(ChineseServers.DataCenters())
            .ToList());
    }

    private static IEnumerable<CsvWorld> GetValidWorlds(IEnumerable<CsvWorld> worlds)
    {
        return worlds
            .Where(w => w.DataCenter > 0)
            .Where(w => w.IsPublic)
            .Where(w => w.RowId != 25); // Chaos (world)
    }

    private class CsvItem
    {
        [Index(0)] public int RowId { get; set; }

        [Name("ItemSearchCategory")] public int ItemSearchCategory { get; set; }

        [Name("StackSize")] public int StackSize { get; set; }
    }

    private class CsvWorld
    {
        [Index(0)] public int RowId { get; set; }

        [Index(1)] public string InternalName { get; set; }

        [Index(2)] public string Name { get; set; }

        [Index(3)] public byte Region { get; set; }

        [Index(4)] public byte UserType { get; set; }

        [Index(5)] public int DataCenter { get; set; }

        [Index(6)] public bool IsPublic { get; set; }
    }

    private class CsvDc
    {
        [Index(0)] public uint RowId { get; set; }

        [Index(1)] public string Name { get; set; }

        [Index(2)] public byte Region { get; set; }
    }
}