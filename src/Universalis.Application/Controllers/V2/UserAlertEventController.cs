using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V2;
using Universalis.Mogboard;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Application.Controllers.V2;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
[RequireMogboardAuthorization]
#if !DEBUG
[ApiExplorerSettings(IgnoreApi = true)]
#endif
public class UserAlertEventController : ControllerBase
{
    private readonly IMogboardTable<UserAlertEvent, UserAlertEventId> _alertEvents;

    public UserAlertEventController(IMogboardTable<UserAlertEvent, UserAlertEventId> alertEvents)
    {
        _alertEvents = alertEvents;
    }

    /// <summary>
    /// Retrieves an alert event. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The alert event could not be found.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("alert-events/{id:guid}")]
    [ApiTag("User alert events")]
    [ProducesResponseType(typeof(UserAlertEventView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return GetV2(id, cancellationToken);
    }

    /// <summary>
    /// Retrieves an alert event. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The alert event could not be found.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/alert-events/{id:guid}")]
    [ApiTag("User alert events")]
    [ProducesResponseType(typeof(UserAlertEventView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid id, CancellationToken cancellationToken = default)
    {
        var alertEventId = new UserAlertEventId(id);
        var alertEvent = await _alertEvents.Get(alertEventId, cancellationToken);
        if (alertEvent == null)
        {
            return NotFound();
        }

        var userAlertEventView = new UserAlertEventView
        {
            Id = alertEvent.Id.ToString(),
            AlertId = alertEvent.AlertId.ToString(),
            TimestampMs = alertEvent.Added.ToUnixTimeMilliseconds().ToString(),
            Data = alertEvent.Data,
        };

        return Ok(userAlertEventView);
    }
}