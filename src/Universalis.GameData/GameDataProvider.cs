using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using LuminaWorld = Lumina.Excel.GeneratedSheets.World;

namespace Universalis.GameData
{
    internal class GameDataProvider : IGameDataProvider
    {
        private const string ExcelLoadError = "Excel sheet could not be loaded!";

        private readonly Lumina.GameData _lumina;

        private readonly Lazy<IReadOnlyDictionary<uint, string>> _availableWorlds;
        private readonly Lazy<IReadOnlyDictionary<string, uint>> _availableWorldsReversed;
        private readonly Lazy<IReadOnlySet<uint>> _availableWorldIds;

        private readonly Lazy<IReadOnlySet<uint>> _marketableItemIds;

        private readonly Lazy<IReadOnlyList<DataCenter>> _dataCenters;

        public GameDataProvider(Lumina.GameData lumina)
        {
            _lumina = lumina;

            _availableWorlds = new Lazy<IReadOnlyDictionary<uint, string>>(LoadAvailableWorlds);
            _availableWorldsReversed = new Lazy<IReadOnlyDictionary<string, uint>>(LoadAvailableWorldsReversed);
            _availableWorldIds = new Lazy<IReadOnlySet<uint>>(LoadAvailableWorldIds);

            _marketableItemIds = new Lazy<IReadOnlySet<uint>>(LoadMarketableItems);

            _dataCenters = new Lazy<IReadOnlyList<DataCenter>>(LoadDataCenters);
        }

        IReadOnlyDictionary<uint, string> IGameDataProvider.AvailableWorlds()
            => _availableWorlds.Value;

        IReadOnlyDictionary<string, uint> IGameDataProvider.AvailableWorldsReversed()
            => _availableWorldsReversed.Value;

        IReadOnlySet<uint> IGameDataProvider.AvailableWorldIds()
            => _availableWorldIds.Value;

        IReadOnlySet<uint> IGameDataProvider.MarketableItemIds()
            => _marketableItemIds.Value;

        IEnumerable<DataCenter> IGameDataProvider.DataCenters()
            => _dataCenters.Value;

        /// <summary>
        /// Gets a read-only dictionary of all available worlds. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlyDictionary<uint, string> LoadAvailableWorlds()
        {
            var worlds = _lumina.GetExcelSheet<LuminaWorld>();
            if (worlds == null)
            {
                throw new InvalidOperationException(ExcelLoadError);
            }

            return worlds
                .Where(w => w.IsPublic)
                .Select(w => new World { Name = w.Name, Id = w.RowId })
                .Concat(ChineseServers.Worlds())
                .ToDictionary(w => w.Id, w => w.Name);
        }

        /// <summary>
        /// Gets a read-only dictionary of all available worlds. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlyDictionary<string, uint> LoadAvailableWorldsReversed()
        {
            var worlds = _lumina.GetExcelSheet<LuminaWorld>();
            if (worlds == null)
            {
                throw new InvalidOperationException(ExcelLoadError);
            }

            return worlds
                .Where(w => w.IsPublic)
                .Select(w => new World { Name = w.Name, Id = w.RowId })
                .Concat(ChineseServers.Worlds())
                .ToDictionary(w => w.Name, w => w.Id);
        }

        /// <summary>
        /// Gets a read-only sorted set of all available world IDs. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlySet<uint> LoadAvailableWorldIds()
        {
            var worlds = _lumina.GetExcelSheet<LuminaWorld>();
            if (worlds == null)
            {
                throw new InvalidOperationException(ExcelLoadError);
            }

            return new SortedSet<uint>(worlds
                .Where(w => w.IsPublic)
                .Select(w => new World { Name = w.Name, Id = w.RowId })
                .Concat(ChineseServers.Worlds())
                .Select(w => w.Id)
                .ToList());
        }

        /// <summary>
        /// Gets a read-only sorted set of all marketable item IDs. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlySet<uint> LoadMarketableItems()
        {
            var items = _lumina.GetExcelSheet<Item>();
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
        /// Gets a list of all data centers. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlyList<DataCenter> LoadDataCenters()
        {
            var dcs = _lumina.GetExcelSheet<WorldDCGroupType>();
            var worlds = _lumina.GetExcelSheet<LuminaWorld>();
            if (dcs == null || worlds == null)
            {
                throw new InvalidOperationException(ExcelLoadError);
            }

            return dcs
                .Where(dc => dc.RowId is >= 1 and < 99)
                .Select(dc => new DataCenter
                {
                    Name = dc.Name,
                    WorldIds = worlds
                        .Where(w => w.IsPublic)
                        .Where(w => w.DataCenter.Row == dc.RowId)
                        .Select(w => w.RowId)
                        .ToArray(),
                })
                .Concat(ChineseServers.DataCenters())
                .ToList();
        }
    }
}
