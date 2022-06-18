using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;

namespace Universalis.Application.Controllers.V1.Extra;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class ContentController : ControllerBase
{
    private readonly ICharacterDbAccess _characterDb;

    public ContentController(ICharacterDbAccess characterDb)
    {
        _characterDb = characterDb;
    }

    /// <summary>
    /// Returns the content object associated with the provided content ID. Please note that this endpoint is largely untested,
    /// and may return inconsistent data at times.
    /// </summary>
    /// <param name="contentId">The ID of the content object to retrieve.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Game entities")]
    [Route("extra/content/{contentId}")]
    [ProducesResponseType(typeof(ContentView), 200)]
    public async Task<IActionResult> Get(string contentId, CancellationToken cancellationToken = default)
    {
        var character = await _characterDb.Retrieve(contentId, cancellationToken);
        if (character == null)
        {
            return Ok(new ContentView());
        }

        return Ok(new ContentView
        {
            ContentId = character.ContentIdSha256,
            ContentType = "player",
            CharacterName = character.Name,
        });
    }

    /// <summary>
    /// Returns the content object associated with the provided content ID. Please note that this endpoint is largely untested,
    /// and may return inconsistent data at times.
    /// </summary>
    /// <param name="contentId">The ID of the content object to retrieve.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Game entities")]
    [Route("v{version:apiVersion}/extra/content/{contentId}")]
    [ProducesResponseType(typeof(ContentView), 200)]
    public Task<IActionResult> GetV2(string contentId, CancellationToken cancellationToken = default)
    {
        return Get(contentId, cancellationToken);
    }
}