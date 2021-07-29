using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/extra/content/{contentId}")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        private readonly IContentDbAccess _contentDb;

        public ContentController(IContentDbAccess contentDb)
        {
            _contentDb = contentDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string contentId)
        {
            var content = await _contentDb.Retrieve(new ContentQuery { ContentId = contentId });
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
}