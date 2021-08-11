using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.GameData;

namespace Universalis.Application.Controllers
{
    public class WorldDcControllerBase : ControllerBase
    {
        protected readonly IGameDataProvider GameData;

        protected WorldDcControllerBase(IGameDataProvider gameData)
        {
            GameData = gameData;
        }

        /// <summary>
        /// Attempts to get a <see cref="WorldDc"/> from the provided input, handling any exceptions.
        /// </summary>
        /// <param name="worldOrDc">The input data.</param>
        /// <param name="worldDc">The resulting <see cref="WorldDc"/>.</param>
        /// <returns><see langword="true" /> if the operation succeeded, otherwise <see langword="false" />.</returns>
        protected bool TryGetWorldDc(string worldOrDc, out WorldDc worldDc)
        {
            return WorldDc.TryParse(worldOrDc, GameData, out worldDc);
        }

        /// <summary>
        /// Attempts to get an array of world IDs from the provided input.
        /// </summary>
        /// <param name="worldDc">The input data.</param>
        /// <param name="worldIds">The resulting array of world IDs.</param>
        /// <returns><see langword="true" /> if the operation succeeded, otherwise <see langword="false" />.</returns>
        protected bool TryGetWorldIds(WorldDc worldDc, out uint[] worldIds)
        {
            worldIds = worldDc.IsWorld ? new[] { worldDc.WorldId } : Array.Empty<uint>();
            if (!worldDc.IsDc) return true;
            var dataCenter = GameData.DataCenters().FirstOrDefault(dc => dc.Name == worldDc.DcName);
            if (dataCenter == null) return false;
            worldIds = dataCenter.WorldIds;
            return true;
        }
    }
}