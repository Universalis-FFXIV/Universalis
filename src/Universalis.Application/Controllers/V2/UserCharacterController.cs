using System;
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
public class UserCharacterController : ControllerBase
{
    private readonly IMogboardTable<UserCharacter, UserCharacterId> _characters;

    public UserCharacterController(IMogboardTable<UserCharacter, UserCharacterId> characters)
    {
        _characters = characters;
    }

    /// <summary>
    /// Retrieves a characters. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The character could not be found.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("characters/{id:guid}")]
    [ApiTag("User characters")]
    [ProducesResponseType(typeof(UserCharacterView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return GetV2(id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a characters. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The character could not be found.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/characters/{id:guid}")]
    [ApiTag("User characters")]
    [ProducesResponseType(typeof(UserCharacterView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid id, CancellationToken cancellationToken = default)
    {
        var characterId = new UserCharacterId(id);
        var character = await _characters.Get(characterId, cancellationToken);
        if (character == null)
        {
            return NotFound();
        }

        var userRetainerView = new UserCharacterView
        {
            Id = character.Id.ToString(),
            LodestoneId = character.LodestoneId.ToString(),
            UpdatedTimestampMs = character.Updated.ToUnixTimeMilliseconds().ToString(),
            Name = character.Name,
            Server = character.Server,
            Avatar = character.Avatar,
            Main = character.Main,
            Confirmed = character.Confirmed,
        };

        return Ok(userRetainerView);
    }
}