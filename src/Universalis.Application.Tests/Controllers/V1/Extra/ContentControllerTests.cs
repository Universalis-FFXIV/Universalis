using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Views.V1.Extra;
using Universalis.Entities;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra;

public class ContentControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockCharacterDbAccess();
        var controller = new ContentController(dbAccess);

        var character = new Character("2A", "B B", null);

        await dbAccess.Create(character);

        var result = await controller.Get("2A");
        var content = (ContentView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(character.ContentIdSha256, content.ContentId);
        Assert.Equal("player", content.ContentType);
        Assert.Equal(character.Name, content.CharacterName);
    }
}