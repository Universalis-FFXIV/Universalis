using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V3.Miscellaneous;

namespace Universalis.Application.Controllers.V3.Miscellaneous;

[ApiController]
[ApiVersion("3")]
[Route("api")]
public class TimeZoneController
{
    /// <summary>
    /// Returns all installed time zones.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("3")]
    [ApiTag("Available time zones")]
    [Route("v{version:apiVersion}/misc/time-zones")]
    [ProducesResponseType(typeof(IEnumerable<TimeZoneView>), 200)]
    public IEnumerable<TimeZoneView> Get()
    {
        var tzList = TimeZoneInfo.GetSystemTimeZones().Where(tz => tz.HasIanaId).ToArray();
        var toReturn = new List<TimeZoneInfo>();
        for (var i = 0; i < tzList.Length; i++)
        {
            var ok = true;
            for (var j = 0; j < i; j++)
            {
                if (tzList[i].HasSameRules(tzList[j]))
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                toReturn.Add(tzList[i]);
            }
        }
        
        return toReturn
            .Select(tz => new TimeZoneView
            {
                Id = tz.Id,
                UtcOffset = tz.BaseUtcOffset.TotalHours,
                FormattedName = tz.DisplayName,
            });
    }
}