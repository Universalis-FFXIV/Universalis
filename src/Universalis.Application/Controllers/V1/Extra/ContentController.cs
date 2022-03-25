using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Views.V1.Extra;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;

namespace Universalis.Application.Controllers.V1.Extra;

[ApiController]
[Route("api/extra/content/{contentId}")]
public class ContentController : ControllerBase
{
    private readonly IContentDbAccess _contentDb;

    public ContentController(IContentDbAccess contentDb)
    {
        _contentDb = contentDb;
    }

    /// <summary>
    /// Returns the content object associated with the provided content ID. Please note that this endpoint is largely untested,
    /// and may return inconsistent data at times.
    /// </summary>
    /// <param name="contentId">The ID of the content object to retrieve.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [ProducesResponseType(typeof(ContentView), 200)]
    public async Task<IActionResult> Get(string contentId, CancellationToken cancellationToken = default)
    {
        var content = await _contentDb.Retrieve(new ContentQuery { ContentId = contentId }, cancellationToken);
        if (content == null)
        {
            return Ok(new ContentView());
        }

        return Ok(new ContentView
        {
            ContentId = content.ContentId,
            ContentType = content.ContentType,
            CharacterName = content.CharacterName,
        });
    }
}