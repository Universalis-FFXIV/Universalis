using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace Universalis.GameData
{
    internal class GameDataProvider : IGameDataProvider
    {
        private readonly Lumina.GameData _lumina;

        private readonly Lazy<IReadOnlyDictionary<uint, string>> _availableWorlds;
        private readonly Lazy<ImmutableSortedSet<uint>> _marketableItems;

        public GameDataProvider(Lumina.GameData lumina)
        {
            _lumina = lumina;

            _availableWorlds = new Lazy<IReadOnlyDictionary<uint, string>>(LoadAvailableWorlds);
            _marketableItems = new Lazy<ImmutableSortedSet<uint>>(LoadMarketableItems);
        }

        IReadOnlyDictionary<uint, string> IGameDataProvider.AvailableWorlds()
        {
            return _availableWorlds.Value;
        }

        ImmutableSortedSet<uint> IGameDataProvider.MarketableItems()
        {
            return _marketableItems.Value;
        }

        /// <summary>
        /// Gets a read-only dictionary of all available worlds. Intended for use in the lazily-loaded member.
        /// </summary>
        private IReadOnlyDictionary<uint, string> LoadAvailableWorlds()
        {
            var worlds = _lumina.GetExcelSheet<World>();
            if (worlds == null)
            {
                throw new InvalidOperationException("Excel sheet could not be loaded!");
            }

            return worlds
                .Where(w => w.IsPublic)
                .ToDictionary(w => w.RowId, w => (string)w.Name);
        }

        /// <summary>
        /// Gets an immutable sorted set of all marketable item IDs. Intended for use in the lazily-loaded member.
        /// </summary>
        private ImmutableSortedSet<uint> LoadMarketableItems()
        {
            var items = _lumina.GetExcelSheet<Item>();
            if (items == null)
            {
                throw new InvalidOperationException("Excel sheet could not be loaded!");
            }

            return items
                .Where(i => i.ItemSearchCategory.Value?.RowId >= 1)
                .Select(i => i.RowId)
                .ToImmutableSortedSet();
        }
    }
}
