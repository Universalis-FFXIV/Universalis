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
public class UserAlertController : ControllerBase
{
    private readonly IMogboardTable<UserAlert, UserAlertId> _alerts;

    public UserAlertController(IMogboardTable<UserAlert, UserAlertId> alerts)
    {
        _alerts = alerts;
    }

    /// <summary>
    /// Retrieves an alert. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The alert could not be found.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("alerts/{id:guid}")]
    [ApiTag("User alerts")]
    [ProducesResponseType(typeof(UserAlertView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return GetV2(id, cancellationToken);
    }

    /// <summary>
    /// Retrieves an alert. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The alert could not be found.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/alerts/{id:guid}")]
    [ApiTag("User alerts")]
    [ProducesResponseType(typeof(UserAlertView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid id, CancellationToken cancellationToken = default)
    {
        var alertId = new UserAlertId(id);
        var alert = await _alerts.Get(alertId, cancellationToken);
        if (alert == null)
        {
            return NotFound();
        }

        var userAlertView = new UserAlertView
        {
            Id = alert.Id.ToString(),
            ItemId = alert.ItemId,
            CreatedTimestampMs = alert.Added.ToUnixTimeMilliseconds().ToString(),
            LastCheckedTimestampMs = alert.Added.ToUnixTimeMilliseconds().ToString(),
            Name = alert.Name,
            Server = alert.Server,
            ExpiryTimestampMs = alert.Expiry.ToUnixTimeMilliseconds().ToString(),
            TriggerConditions = alert.TriggerConditions!.ToArray(),
            TriggerType = alert.TriggerType,
            TriggerLastSentTimestampMs = alert.TriggerLastSent.ToUnixTimeMilliseconds().ToString(),
            TriggerDataCenter = alert.TriggerDataCenter,
            TriggerNq = alert.TriggerNq,
            TriggerHq = alert.TriggerHq,
            TriggerActive = alert.TriggerActive,
        };

        return Ok(userAlertView);
    }
}