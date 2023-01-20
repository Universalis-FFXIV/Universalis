using Lumina;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using LuminaWorld = Lumina.Excel.GeneratedSheets.World;

namespace Universalis.GameData;

internal class LuminaGameDataProvider : IGameDataProvider
{
    private const string ExcelLoadError = "Excel sheet could not be loaded!";

    private readonly IReadOnlyDictionary<int, string> _availableWorlds;
    private readonly IReadOnlyDictionary<string, int> _availableWorldsReversed;
    private readonly IReadOnlySet<int> _availableWorldIds;

    private readonly IReadOnlySet<int> _marketableItemIds;
    private readonly IReadOnlyDictionary<int, int> _marketableItemStackSizes;

    private readonly IReadOnlyList<DataCenter> _dataCenters;

    public LuminaGameDataProvider(string sqpack)
    {
        // All data is loaded immediately, and then the Lumina instance is
        // garbage-collected. Lumina is a bit of a memory hog and we only
        // need to load the data once.
        var lumina = new Lumina.GameData(sqpack, new LuminaOptions { PanicOnSheetChecksumMismatch = false });

        _availableWorlds = LoadAvailableWorlds(lumina);
        _availableWorldsReversed = LoadAvailableWorldsReversed(lumina);
        _availableWorldIds = LoadAvailableWorldIds(lumina);

        _marketableItemIds = LoadMarketableItems(lumina);
        _marketableItemStackSizes = LoadMarketableItemStackSizes(lumina);

        _dataCenters = LoadDataCenters(lumina);
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

    /// <summary>
    /// Gets a read-only dictionary of all available worlds.
    /// </summary>
    private static IReadOnlyDictionary<int, string> LoadAvailableWorlds(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = Convert.ToInt32(w.RowId) })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Id, w => w.Name);
    }

    /// <summary>
    /// Gets a read-only dictionary of all available worlds.
    /// </summary>
    private static IReadOnlyDictionary<string, int> LoadAvailableWorldsReversed(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = Convert.ToInt32(w.RowId) })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Name, w => w.Id);
    }

    /// <summary>
    /// Gets a read-only sorted set of all available world IDs.
    /// </summary>
    private static IReadOnlySet<int> LoadAvailableWorldIds(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return new SortedSet<int>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = Convert.ToInt32(w.RowId) })
            .Concat(ChineseServers.Worlds())
            .Select(w => Convert.ToInt32(w.Id))
            .ToList());
    }

    /// <summary>
    /// Gets a read-only sorted set of all marketable item IDs.
    /// </summary>
    private static IReadOnlySet<int> LoadMarketableItems(Lumina.GameData lumina)
    {
        var items = lumina.GetExcelSheet<Item>();
        if (items == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return new SortedSet<int>(items
            .Where(i => i.ItemSearchCategory.Value?.RowId >= 1)
            .Select(i => Convert.ToInt32(i.RowId))
            .ToList());
    }
    
    /// <summary>
    /// Gets a read-only dictionary of the stack size limits for all marketable items.
    /// </summary>
    private static IReadOnlyDictionary<int, int> LoadMarketableItemStackSizes(Lumina.GameData lumina)
    {
        var items = lumina.GetExcelSheet<Item>();
        if (items == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return items
            .Where(i => i.ItemSearchCategory.Value?.RowId >= 1)
            .ToDictionary(i => Convert.ToInt32(i.RowId), i => Convert.ToInt32(i.StackSize));
    }

    /// <summary>
    /// Gets a list of all data centers.
    /// </summary>
    private static IReadOnlyList<DataCenter> LoadDataCenters(Lumina.GameData lumina)
    {
        var dcs = lumina.GetExcelSheet<WorldDCGroupType>();
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (dcs == null || worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return dcs
            .Where(dc => dc.RowId is > 0 and < 99)
            .Select(dc => new DataCenter
            {
                Name = dc.Name,
                Region = Regions.Map[dc.Region],
                WorldIds = GetValidWorlds(worlds)
                    .Where(w => w.DataCenter.Row == dc.RowId)
                    .Select(w => Convert.ToInt32(w.RowId))
                    .ToArray(),
            })
            .Where(dc => dc.WorldIds.Length > 0)
            .Concat(ChineseServers.DataCenters())
            .ToList();
    }

    private static IEnumerable<LuminaWorld> GetValidWorlds(IEnumerable<LuminaWorld> worlds)
    {
        return worlds
            .Where(w => w.DataCenter.Row > 0)
            .Where(w => w.IsPublic)
            .Where(w => w.RowId != 25); // Chaos (world)
    }
}
