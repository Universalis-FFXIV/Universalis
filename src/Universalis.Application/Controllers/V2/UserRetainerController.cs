using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
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
public class UserRetainerController : ControllerBase
{
    private readonly IMogboardTable<UserRetainer, UserRetainerId> _retainers;

    public UserRetainerController(IMogboardTable<UserRetainer, UserRetainerId> retainers)
    {
        _retainers = retainers;
    }

    /// <summary>
    /// Retrieves a retainer. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The retainer could not be found.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("retainers/{id:guid}")]
    [ApiTag("User retainers")]
    [ProducesResponseType(typeof(UserRetainerView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return GetV2(id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a retainer. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The retainer could not be found.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/retainers/{id:guid}")]
    [ApiTag("User retainers")]
    [ProducesResponseType(typeof(UserRetainerView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid id, CancellationToken cancellationToken = default)
    {
        var retainerId = new UserRetainerId(id);
        var retainer = await _retainers.Get(retainerId, cancellationToken);
        if (retainer == null)
        {
            return NotFound();
        }

        var userRetainerView = new UserRetainerView
        {
            Id = retainer.Id.ToString(),
            CreatedTimestampMs = retainer.Added.ToUnixTimeMilliseconds().ToString(),
            UpdatedTimestampMs = retainer.Updated.ToUnixTimeMilliseconds().ToString(),
            Name = retainer.Name,
            Server = retainer.Server,
            Avatar = retainer.Avatar,
            Confirmed = retainer.Confirmed,
        };

        return Ok(userRetainerView);
    }
}