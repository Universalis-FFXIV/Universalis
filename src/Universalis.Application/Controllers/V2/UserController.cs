using Microsoft.AspNetCore.Mvc;
using System;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V2;
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
public class UserController : ControllerBase
{
    /// <summary>
    /// Retrieves the current user. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("users/@me")]
    [ApiTag("Current user")]
    [ProducesResponseType(typeof(UserView), 200)]
    [ProducesResponseType(403)]
    public IActionResult Get()
    {
        return GetV2();
    }

    /// <summary>
    /// Retrieves the current user. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/users/@me")]
    [ApiTag("Current user")]
    [ProducesResponseType(typeof(UserView), 200)]
    [ProducesResponseType(403)]
    public IActionResult GetV2()
    {
        var user = (MogboardUser?)HttpContext.Items["user"];
        if (user == null) throw new ArgumentNullException(nameof(user));

        UserView.SsoView? ssoView = null;

        var discordSso = user.GetDiscordSso();
        if (discordSso != null)
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            ssoView ??= new UserView.SsoView();
            ssoView.Discord = new UserView.SsoView.DiscordSsoView
            {
                Id = discordSso.Id!,
                Avatar = discordSso.Avatar!,
            };
        }

        var userView = new UserView
        {
            Id = user.GetId().ToString(),
            CreatedTimestampMs = user.GetCreationTime().ToUnixTimeMilliseconds().ToString(),
            LastOnlineTimestampMs = user.GetLastOnlineTime().ToUnixTimeMilliseconds().ToString(),
            Username = user.GetUsername(),
            Email = user.GetEmail(),
            Avatar = user.GetAvatar()!,
            Sso = ssoView!,
        };

        return Ok(userView);
    }
}