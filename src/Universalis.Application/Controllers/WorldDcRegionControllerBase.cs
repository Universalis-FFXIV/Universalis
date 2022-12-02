using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

public class WorldDcRegionControllerBase : ControllerBase
{
    protected readonly IGameDataProvider GameData;

    protected WorldDcRegionControllerBase(IGameDataProvider gameData)
    {
        GameData = gameData;
    }

    /// <summary>
    /// Attempts to get a <see cref="WorldDcRegion"/> from the provided input, handling any exceptions.
    /// </summary>
    /// <param name="worldOrDc">The input data.</param>
    /// <param name="worldDcRegion">The resulting <see cref="WorldDcRegion"/>.</param>
    /// <returns><see langword="true" /> if the operation succeeded, otherwise <see langword="false" />.</returns>
    protected bool TryGetWorldDc(string worldOrDc, out WorldDcRegion worldDcRegion)
    {
        return WorldDcRegion.TryParse(worldOrDc, GameData, out worldDcRegion);
    }

    /// <summary>
    /// Attempts to get an array of world IDs from the provided input.
    /// </summary>
    /// <param name="worldDcRegion">The input data.</param>
    /// <param name="worldIds">The resulting array of world IDs.</param>
    /// <returns><see langword="true" /> if the operation succeeded, otherwise <see langword="false" />.</returns>
    protected bool TryGetWorldIds(WorldDcRegion worldDcRegion, out int[] worldIds)
    {
        worldIds = Array.Empty<int>();

        if (worldDcRegion.IsWorld)
        {
            worldIds = new[] { worldDcRegion.WorldId };
            return true;
        }

        if (worldDcRegion.IsDc)
        {
            var dataCenter = GameData.DataCenters().FirstOrDefault(dc => dc.Name == worldDcRegion.DcName);
            if (dataCenter == null)
            {
                return false;
            }

            worldIds = dataCenter.WorldIds;
            return true;
        }

        if (worldDcRegion.IsRegion)
        {
            worldIds = GameData.DataCenters()
                .Where(dc => dc.Region == worldDcRegion.RegionName)
                .SelectMany(dc => dc.WorldIds)
                .ToArray();
            return true;
        }

        return false;
    }
}