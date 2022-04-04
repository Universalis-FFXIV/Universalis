using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V2;
using Universalis.Mogboard;
using Universalis.Mogboard.Doctrine;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;
using Universalis.Mogboard.Identity;

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
            Name = alert.Name!,
            Server = alert.Server!,
            ExpiryTimestampMs = alert.Expiry.ToUnixTimeMilliseconds().ToString(),
            TriggerConditions = alert.TriggerConditions!.ToList(),
            TriggerType = alert.TriggerType!,
            TriggerLastSentTimestampMs = alert.TriggerLastSent.ToUnixTimeMilliseconds().ToString(),
            TriggerDataCenter = alert.TriggerDataCenter,
            TriggerNq = alert.TriggerNq,
            TriggerHq = alert.TriggerHq,
            TriggerActive = alert.TriggerActive,
        };

        return Ok(userAlertView);
    }

    /// <summary>
    /// Creates a new user alert.
    /// </summary>
    /// <param name="create">The alert parameters.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [MapToApiVersion("1")]
    [Route("alerts")]
    [ApiTag("User alerts")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Post([FromBody] UserAlertCreateView create,
        CancellationToken cancellationToken = default)
    {
        return PostV2(create, cancellationToken);
    }

    /// <summary>
    /// Creates a new user alert.
    /// </summary>
    /// <param name="create">The alert parameters.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/alerts")]
    [ApiTag("User alerts")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostV2([FromBody] UserAlertCreateView create,
        CancellationToken cancellationToken = default)
    {
        var user = (MogboardUser)HttpContext.Items["user"];
        if (user == null) throw new InvalidOperationException();

        await _alerts.Create(new UserAlert
        {
            Id = new UserAlertId(),
            UserId = user.GetId(),
            Uniq = "",
            ItemId = create.AlertItemId ?? throw new InvalidOperationException(),
            Added = DateTimeOffset.UtcNow,
            ActiveTime = DateTimeOffset.UtcNow,
            LastChecked = DateTimeOffset.FromUnixTimeSeconds(0),
            Name = create.AlertName ?? throw new InvalidOperationException(),
            Expiry = DateTimeOffset.UtcNow + new TimeSpan(30, 0, 0, 0),
            TriggerConditions = new DoctrineArray<string>(create.AlertTriggers ?? throw new InvalidOperationException()),
            TriggerType = "",
            TriggerLastSent = DateTimeOffset.FromUnixTimeSeconds(0),
            TriggersSent = 0,
            TriggerAction = "continue",
            TriggerDataCenter = create.AlertDc ?? false,
            TriggerHq = create.AlertHq ?? false,
            TriggerNq = create.AlertNq ?? false,
            TriggerActive = true,
            NotifiedViaEmail = create.AlertNotifyEmail ?? false,
            NotifiedViaDiscord = create.AlertNotifyDiscord ?? false,
            KeepUpdated = false,
        }, cancellationToken);

        return Ok();
    }
}