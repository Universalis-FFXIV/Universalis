using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class UploadControllerTests
{
    private class TestResources
    {
        public IFlaggedUploaderDbAccess FlaggedUploaders { get; private init; }
        public ITrustedSourceDbAccess TrustedSources { get; private init; }
        public UploadController Controller { get; private init; }
        
        public static TestResources Create(IEnumerable<IUploadBehavior> uploadBehaviors)
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var controller = new UploadController(trustedSources, flaggedUploaders, uploadBehaviors);
            return new TestResources
            {
                FlaggedUploaders = flaggedUploaders,
                TrustedSources = trustedSources,
                Controller = controller,
            };
        }
    }
    
    [Fact]
    public async Task Controller_Post_Succeeds()
    {
        var test = TestResources.Create(Enumerable.Empty<IUploadBehavior>());

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var upload = new UploadParameters { UploaderId = "ffff" };

        var result = await test.Controller.Post(key, upload);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Fails_WithBadAuthorization()
    {
        var test = TestResources.Create(Enumerable.Empty<IUploadBehavior>());

        var upload = new UploadParameters { UploaderId = "ffff" };

        var result = await test.Controller.Post("iudh9832h9c32huwh", upload);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Fails_WithNoUploaderId()
    {
        var test = TestResources.Create(Enumerable.Empty<IUploadBehavior>());

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var upload = new UploadParameters();

        var result = await test.Controller.Post(key, upload);
        Assert.IsType<BadRequestResult>(result);
    }
    
    [Fact]
    public async Task Controller_Post_Fails_WithNoUploadPermissions()
    {
        var test = TestResources.Create(Enumerable.Empty<IUploadBehavior>());

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", false));
        }

        var upload = new UploadParameters { UploaderId = "ffff" };

        var result = await test.Controller.Post(key, upload);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Controller_Post_FailsSilently_WhenFlagged()
    {
        var test = TestResources.Create(Enumerable.Empty<IUploadBehavior>());

        const string key = "blah";
        const string uploaderId = "ffff";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        string uploaderIdHash;
        using (var sha256 = SHA256.Create())
        {
            uploaderIdHash = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(uploaderId)));
        }

        await test.FlaggedUploaders.Create(new FlaggedUploader(uploaderIdHash));

        var upload = new UploadParameters { UploaderId = uploaderId };

        var result = await test.Controller.Post(key, upload);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Validators_Run_First()
    {
        var content = new MockCharacterDbAccess();
        var gameData = new MockGameDataProvider();
        var worldUploadCounts = new MockWorldUploadCountDbAccess();
        
        var test = TestResources.Create(new List<IUploadBehavior>
        {
            new PlayerContentUploadBehavior(content),
            new WorldIdUploadBehavior(gameData, worldUploadCounts),
        });

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var upload1 = new UploadParameters
        {
            WorldId = 0,
            ContentId = "afffff",
            CharacterName = "Bubu Bubba",
            UploaderId = "affffe",
        };

        var result1 = await test.Controller.Post(key, upload1);
        Assert.IsType<NotFoundObjectResult>(result1);

        var upload2 = new UploadParameters
        {
            WorldId = 74,
            ContentId = "afffff",
            CharacterName = "Bubu Bubba",
            UploaderId = "affffe",
        };

        var result2 = await test.Controller.Post(key, upload2);
        Assert.IsType<OkObjectResult>(result2);
    }
}