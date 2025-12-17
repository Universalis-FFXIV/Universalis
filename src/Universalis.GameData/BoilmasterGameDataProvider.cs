using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Universalis.GameData;

/// <summary>
/// Game data provider that uses XIVAPI v2 (boilmaster) for FFXIV game data.
/// </summary>
public class BoilmasterGameDataProvider : IGameDataProvider
{
    private IReadOnlyDictionary<int, string> _availableWorlds;
    private IReadOnlyDictionary<string, int> _availableWorldsReversed;
    private IReadOnlySet<int> _availableWorldIds;

    private IReadOnlySet<int> _marketableItemIds;
    private IReadOnlyDictionary<int, int> _marketableItemStackSizes;

    private IReadOnlyList<DataCenter> _dataCenters;

    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public BoilmasterGameDataProvider(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        Task.Run(async () =>
        {
            try
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

                _logger.LogInformation("BoilmasterGameDataProvider initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize BoilmasterGameDataProvider");
                throw;
            }
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

    private async Task<IList<ApiWorld>> GetWorlds()
    {
        _logger.LogDebug("Fetching worlds from XIVAPI");

        var allWorlds = new List<ApiWorld>();
        var after = 0;
        const int pageSize = 500;
        const string fields = "Name,InternalName,Region,UserType,DataCenter,IsPublic";

        while (true)
        {
            var response =
                await _http.GetAsync(
                    $"https://v2.xivapi.com/api/sheet/World?limit={pageSize}&after={after}&fields={fields}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ApiWorld>>(content, _jsonOptions);

            if (apiResponse?.Rows == null || apiResponse.Rows.Count == 0)
                break;

            allWorlds.AddRange(apiResponse.Rows);

            // Break if we got fewer results than requested (last page)
            if (apiResponse.Rows.Count < pageSize)
                break;

            after += pageSize;

            // Rate limiting
            await Task.Delay(100);
        }

        _logger.LogInformation("Fetched {Count} worlds from XIVAPI", allWorlds.Count);
        return allWorlds;
    }

    private async Task<IList<ApiItem>> GetItems()
    {
        _logger.LogDebug("Fetching items from XIVAPI");

        var allItems = new List<ApiItem>();
        var after = 0;
        const int pageSize = 500;
        const string fields = "Name,ItemSearchCategory,StackSize";

        while (true)
        {
            var response =
                await _http.GetAsync(
                    $"https://v2.xivapi.com/api/sheet/Item?limit={pageSize}&after={after}&fields={fields}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ApiItem>>(content, _jsonOptions);

            if (apiResponse?.Rows == null || apiResponse.Rows.Count == 0)
                break;

            allItems.AddRange(apiResponse.Rows);

            // Break if we got fewer results than requested (last page)
            if (apiResponse.Rows.Count < pageSize)
                break;

            after += pageSize;

            // Rate limiting
            await Task.Delay(100);
        }

        _logger.LogInformation("Fetched {Count} items from XIVAPI", allItems.Count);
        return allItems;
    }

    private async Task<IList<ApiDataCenter>> GetDataCenters()
    {
        _logger.LogDebug("Fetching data centers from XIVAPI");

        var allDataCenters = new List<ApiDataCenter>();
        var after = 0;
        const int pageSize = 100;
        const string fields = "Name,Region";

        while (true)
        {
            var response =
                await _http.GetAsync(
                    $"https://v2.xivapi.com/api/sheet/WorldDCGroupType?limit={pageSize}&after={after}&fields={fields}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ApiDataCenter>>(content, _jsonOptions);

            if (apiResponse?.Rows == null || apiResponse.Rows.Count == 0)
                break;

            allDataCenters.AddRange(apiResponse.Rows);

            // Break if we got fewer results than requested (last page)
            if (apiResponse.Rows.Count < pageSize)
                break;

            after += pageSize;

            // Rate limiting
            await Task.Delay(100);
        }

        _logger.LogInformation("Fetched {Count} data centers from XIVAPI v2", allDataCenters.Count);
        return allDataCenters;
    }

    private static Task<IReadOnlyDictionary<int, string>> LoadAvailableWorlds(IEnumerable<ApiWorld> worlds)
    {
        return Task.FromResult<IReadOnlyDictionary<int, string>>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Fields.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Concat(KoreanServers.Worlds())
            .Concat(TraditionalChineseServers.Worlds())
            .ToDictionary(w => w.Id, w => w.Name));
    }

    private static Task<IReadOnlyDictionary<string, int>> LoadAvailableWorldsReversed(IEnumerable<ApiWorld> worlds)
    {
        return Task.FromResult<IReadOnlyDictionary<string, int>>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Fields.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Concat(KoreanServers.Worlds())
            .Concat(TraditionalChineseServers.Worlds())
            .ToDictionary(w => w.Name, w => w.Id));
    }

    private static Task<IReadOnlySet<int>> LoadAvailableWorldIds(IEnumerable<ApiWorld> worlds)
    {
        return Task.FromResult<IReadOnlySet<int>>(new SortedSet<int>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Fields.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Concat(KoreanServers.Worlds())
            .Concat(TraditionalChineseServers.Worlds())
            .Select(w => w.Id)
            .ToList()));
    }

    private static Task<IReadOnlySet<int>> LoadMarketableItems(IEnumerable<ApiItem> items)
    {
        return Task.FromResult<IReadOnlySet<int>>(new SortedSet<int>(items
            .Where(i => i.Fields.ItemSearchCategory.RowId >= 1)
            .Select(i => i.RowId)
            .ToList()));
    }

    private static Task<IReadOnlyDictionary<int, int>> LoadMarketableItemStackSizes(IEnumerable<ApiItem> items)
    {
        return Task.FromResult<IReadOnlyDictionary<int, int>>(items
            .Where(i => i.Fields.ItemSearchCategory.RowId >= 1)
            .ToDictionary(i => i.RowId, i => i.Fields.StackSize));
    }

    private static Task<IReadOnlyList<DataCenter>> LoadDataCenters(IEnumerable<ApiWorld> worlds,
        IEnumerable<ApiDataCenter> dcs)
    {
        // Build a mapping from world name to world ID
        var worldNameToId = GetValidWorlds(worlds)
            .ToDictionary(w => w.Fields.Name, w => w.RowId);

        return Task.FromResult<IReadOnlyList<DataCenter>>(dcs
            .Where(dc => dc.RowId is > 0 and < 99)
            .Select(dc => new DataCenter
            {
                Name = dc.Fields.Name,
                Region = Regions.Map[dc.Fields.Region],
                // Use hardcoded mapping instead of w.Fields.DataCenter.RowId, which is broken upstream
                WorldIds = GlobalServers.WorldToDataCenter
                    .Where(kvp => kvp.Value == dc.Fields.Name)
                    .Where(kvp => worldNameToId.ContainsKey(kvp.Key))
                    .Select(kvp => worldNameToId[kvp.Key])
                    .ToArray(),
            })
            .Where(dc => dc.WorldIds.Length > 0)
            .Concat(ChineseServers.DataCenters())
            .Concat(KoreanServers.DataCenters())
            .Concat(TraditionalChineseServers.DataCenters())
            .ToList());
    }

    private static IEnumerable<ApiWorld> GetValidWorlds(IEnumerable<ApiWorld> worlds)
    {
        // The new Dynamis worlds are currently not marked as public, despite being public
        // https://na.finalfantasyxiv.com/lodestone/topics/detail/93750e4b0d6d5a4faf6a00dee199082392f1a754
        var nonPublicButActualPublicWorlds = new[] { 408, 409, 410, 411 };
        return worlds
            .Where(w => w.Fields.DataCenter.RowId > 0)
            .Where(w => w.Fields.IsPublic || nonPublicButActualPublicWorlds.Contains(w.RowId))
            .Where(w => w.RowId != 25); // Chaos (world)
    }

    // API Response Models
    private class ApiResponse<T>
    {
        public string Schema { get; set; } = string.Empty;
        public List<T> Rows { get; set; } = new();
    }

    private class ApiWorld
    {
        [JsonPropertyName("row_id")] public int RowId { get; set; }
        public ApiWorldFields Fields { get; set; } = new();
    }

    private class ApiWorldFields
    {
        public string Name { get; set; } = string.Empty;
        public string? InternalName { get; set; }
        public byte Region { get; set; }
        public byte UserType { get; set; }
        public ApiDataCenter DataCenter { get; set; }
        public bool IsPublic { get; set; }
    }

    private class ApiItem
    {
        [JsonPropertyName("row_id")] public int RowId { get; set; }
        public ApiItemFields Fields { get; set; } = new();
    }

    private class ApiItemFields
    {
        public string Name { get; set; } = string.Empty;
        public ApiItemSearchCategory ItemSearchCategory { get; set; }
        public int StackSize { get; set; } = 1; // Default to 1 if not provided
    }

    private class ApiItemSearchCategory
    {
        [JsonPropertyName("row_id")] public int RowId { get; set; }
    }

    private class ApiDataCenter
    {
        [JsonPropertyName("row_id")] public uint RowId { get; set; }
        public ApiDataCenterFields Fields { get; set; } = new();
    }

    private class ApiDataCenterFields
    {
        public string Name { get; set; } = string.Empty;
        public byte Region { get; set; }
    }
}