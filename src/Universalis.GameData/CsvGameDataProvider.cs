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
    private readonly IReadOnlyDictionary<uint, string> _availableWorlds;
    private readonly IReadOnlyDictionary<string, uint> _availableWorldsReversed;
    private readonly IReadOnlySet<uint> _availableWorldIds;

    private readonly IReadOnlySet<uint> _marketableItemIds;

    private readonly IReadOnlyList<DataCenter> _dataCenters;

    private readonly HttpClient _http;

    public CsvGameDataProvider(HttpClient http)
    {
        _http = http;

        _availableWorlds = LoadAvailableWorlds().GetAwaiter().GetResult();
        _availableWorldsReversed = LoadAvailableWorldsReversed().GetAwaiter().GetResult();
        _availableWorldIds = LoadAvailableWorldIds().GetAwaiter().GetResult();

        _marketableItemIds = LoadMarketableItems().GetAwaiter().GetResult();

        _dataCenters = LoadDataCenters().GetAwaiter().GetResult();
    }

    IReadOnlyDictionary<uint, string> IGameDataProvider.AvailableWorlds()
        => _availableWorlds;

    IReadOnlyDictionary<string, uint> IGameDataProvider.AvailableWorldsReversed()
        => _availableWorldsReversed;

    IReadOnlySet<uint> IGameDataProvider.AvailableWorldIds()
        => _availableWorldIds;

    IReadOnlySet<uint> IGameDataProvider.MarketableItemIds()
        => _marketableItemIds;

    IEnumerable<DataCenter> IGameDataProvider.DataCenters()
        => _dataCenters;

    private async Task<IReadOnlyDictionary<uint, string>> LoadAvailableWorlds()
    {
        var csvData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        for (var i = 0; i < 3; i++) await csv.ReadAsync();
        var worlds = csv.GetRecords<CsvWorld>();
        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Id, w => w.Name);
    }

    private async Task<IReadOnlyDictionary<string, uint>> LoadAvailableWorldsReversed()
    {
        var csvData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        for (var i = 0; i < 3; i++) await csv.ReadAsync();
        var worlds = csv.GetRecords<CsvWorld>();
        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Name, w => w.Id);
    }

    private async Task<IReadOnlySet<uint>> LoadAvailableWorldIds()
    {
        var csvData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        for (var i = 0; i < 3; i++) await csv.ReadAsync();
        var worlds = csv.GetRecords<CsvWorld>();
        return new SortedSet<uint>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Select(w => w.Id)
            .ToList());
    }

    private async Task<IReadOnlySet<uint>> LoadMarketableItems()
    {
        var csvData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv");
        using var reader = new StreamReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        await csv.ReadAsync();
        await csv.ReadAsync();
        csv.ReadHeader();
        await csv.ReadAsync();
        var items = csv.GetRecords<CsvItem>();
        return new SortedSet<uint>(items
            .Where(i => i.ItemSearchCategory >= 1)
            .Select(i => i.RowId)
            .ToList());
    }

    private async Task<IReadOnlyList<DataCenter>> LoadDataCenters()
    {
        var worldData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
        using var worldReader = new StreamReader(worldData);
        using var worldCsv = new CsvReader(worldReader, CultureInfo.InvariantCulture);

        var dcData =
            await _http.GetStreamAsync("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/WorldDCGroupType.csv");
        using var dcReader = new StreamReader(dcData);
        using var dcCsv = new CsvReader(dcReader, CultureInfo.InvariantCulture);

        for (var i = 0; i < 3; i++) await worldCsv.ReadAsync();
        for (var i = 0; i < 3; i++) await dcCsv.ReadAsync();

        var worlds = worldCsv.GetRecords<CsvWorld>();
        var dcs = dcCsv.GetRecords<CsvDc>();
        return dcs
            .Where(dc => dc.RowId is >= 1 and < 99)
            .Select(dc => new DataCenter
            {
                Name = dc.Name,
                WorldIds = GetValidWorlds(worlds)
                    .Where(w => w.DataCenter == dc.RowId)
                    .Select(w => w.RowId)
                    .ToArray(),
            })
            .Concat(ChineseServers.DataCenters())
            .ToList();
    }

    private static IEnumerable<CsvWorld> GetValidWorlds(IEnumerable<CsvWorld> worlds)
    {
        return worlds
            .Where(w => w.DataCenter is >= 1 and < 99)
            .Where(w => w.RowId != 25); // Chaos (world)
    }

    private class CsvItem
    {
        [Index(0)]
        public uint RowId { get; set; }

        [Name("ItemSearchCategory")]
        public uint ItemSearchCategory { get; set; }
    }

    private class CsvWorld
    {
        [Index(0)]
        public uint RowId { get; set; }

        [Index(1)]
        public string InternalName { get; set; }

        [Index(2)]
        public string Name { get; set; }

        [Index(3)]
        public byte Region { get; set; }

        [Index(4)]
        public byte UserType { get; set; }

        [Index(5)]
        public int DataCenter { get; set; }

        [Index(6)]
        public bool IsPublic { get; set; }
    }

    private class CsvDc
    {
        [Index(0)]
        public uint RowId { get; set; }

        [Index(1)]
        public string Name { get; set; }

        [Index(2)]
        public byte Region { get; set; }
    }
}