using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Views;
using Universalis.Entities;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra
{
    public class ContentControllerTests
    {
        [Fact]
        public async Task Controller_Get_Succeeds()
        {
            var dbAccess = new MockContentDbAccess();
            var controller = new ContentController(dbAccess);

            var document = new Content
            {
                ContentId = "2A",
                ContentType = ContentKind.Player,
                CharacterName = "B B",
            };

            await dbAccess.Create(document);

            var result = await controller.Get("2A");
            var content = (ContentView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Equal(document.ContentId, content.ContentId);
            Assert.Equal(document.ContentType, content.ContentType);
            Assert.Equal(document.CharacterName, content.CharacterName);
        }
    }
}