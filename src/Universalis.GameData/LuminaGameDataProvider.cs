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

    private readonly IReadOnlyDictionary<uint, string> _availableWorlds;
    private readonly IReadOnlyDictionary<string, uint> _availableWorldsReversed;
    private readonly IReadOnlySet<uint> _availableWorldIds;

    private readonly IReadOnlySet<uint> _marketableItemIds;
    private readonly IReadOnlyDictionary<uint, uint> _marketableItemStackSizes;

    private readonly IReadOnlyList<DataCenter> _dataCenters;

    public LuminaGameDataProvider(string sqpack)
    {
        var lumina = new Lumina.GameData(sqpack, new LuminaOptions { PanicOnSheetChecksumMismatch = false });

        _availableWorlds = LoadAvailableWorlds(lumina);
        _availableWorldsReversed = LoadAvailableWorldsReversed(lumina);
        _availableWorldIds = LoadAvailableWorldIds(lumina);

        _marketableItemIds = LoadMarketableItems(lumina);
        _marketableItemStackSizes = LoadMarketableItemStackSizes(lumina);

        _dataCenters = LoadDataCenters(lumina);
    }

    IReadOnlyDictionary<uint, string> IGameDataProvider.AvailableWorlds()
        => _availableWorlds;

    IReadOnlyDictionary<string, uint> IGameDataProvider.AvailableWorldsReversed()
        => _availableWorldsReversed;

    IReadOnlySet<uint> IGameDataProvider.AvailableWorldIds()
        => _availableWorldIds;

    IReadOnlySet<uint> IGameDataProvider.MarketableItemIds()
        => _marketableItemIds;

    IReadOnlyDictionary<uint, uint> IGameDataProvider.MarketableItemStackSizes()
        => _marketableItemStackSizes;

    IEnumerable<DataCenter> IGameDataProvider.DataCenters()
        => _dataCenters;

    /// <summary>
    /// Gets a read-only dictionary of all available worlds.
    /// </summary>
    private static IReadOnlyDictionary<uint, string> LoadAvailableWorlds(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Id, w => w.Name);
    }

    /// <summary>
    /// Gets a read-only dictionary of all available worlds.
    /// </summary>
    private static IReadOnlyDictionary<string, uint> LoadAvailableWorldsReversed(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .ToDictionary(w => w.Name, w => w.Id);
    }

    /// <summary>
    /// Gets a read-only sorted set of all available world IDs.
    /// </summary>
    private static IReadOnlySet<uint> LoadAvailableWorldIds(Lumina.GameData lumina)
    {
        var worlds = lumina.GetExcelSheet<LuminaWorld>();
        if (worlds == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return new SortedSet<uint>(GetValidWorlds(worlds)
            .Select(w => new World { Name = w.Name, Id = w.RowId })
            .Concat(ChineseServers.Worlds())
            .Select(w => w.Id)
            .ToList());
    }

    /// <summary>
    /// Gets a read-only sorted set of all marketable item IDs.
    /// </summary>
    private static IReadOnlySet<uint> LoadMarketableItems(Lumina.GameData lumina)
    {
        var items = lumina.GetExcelSheet<Item>();
        if (items == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return new SortedSet<uint>(items
            .Where(i => i.ItemSearchCategory.Value?.RowId >= 1)
            .Select(i => i.RowId)
            .ToList());
    }
    
    /// <summary>
    /// Gets a read-only dictionary of the stack size limits for all marketable items.
    /// </summary>
    private static IReadOnlyDictionary<uint, uint> LoadMarketableItemStackSizes(Lumina.GameData lumina)
    {
        var items = lumina.GetExcelSheet<Item>();
        if (items == null)
        {
            throw new InvalidOperationException(ExcelLoadError);
        }

        return items
            .Where(i => i.ItemSearchCategory.Value?.RowId >= 1)
            .ToDictionary(i => i.RowId, i => i.StackSize);
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
            .Where(dc => dc.RowId is >= 1 and < 99)
            .Select(dc => new DataCenter
            {
                Name = dc.Name,
                WorldIds = GetValidWorlds(worlds)
                    .Where(w => w.DataCenter.Row == dc.RowId)
                    .Select(w => w.RowId)
                    .ToArray(),
            })
            .Concat(ChineseServers.DataCenters())
            .ToList();
    }

    private static IEnumerable<LuminaWorld> GetValidWorlds(IEnumerable<LuminaWorld> worlds)
    {
        return worlds
            .Where(w => w.DataCenter.Row is >= 1 and < 99)
            .Where(w => w.IsPublic)
            .Where(w => w.RowId != 25); // Chaos (world)
    }
}
