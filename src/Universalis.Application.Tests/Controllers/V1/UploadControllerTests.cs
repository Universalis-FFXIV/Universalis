using Microsoft.AspNetCore.Mvc;
using System;
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
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class UploadControllerTests
    {
        [Fact]
        public async Task Controller_Post_Succeeds()
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var controller = new UploadController(trustedSources, flaggedUploaders, Enumerable.Empty<IUploadBehavior>());

            const string key = "blah";
            using (var sha256 = SHA256.Create())
            {
                await trustedSources.Create(new TrustedSource
                {
                    ApiKeySha256 = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
                });
            }

            var upload = new UploadParameters { UploaderId = "ffff" };

            var result = await controller.Post(key, upload);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_Post_Fails_WithBadAuthorization()
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var controller = new UploadController(trustedSources, flaggedUploaders, Enumerable.Empty<IUploadBehavior>());

            var upload = new UploadParameters { UploaderId = "ffff" };

            var result = await controller.Post("iudh9832h9c32huwh", upload);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Controller_Post_Fails_WithNoUploaderId()
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var controller = new UploadController(trustedSources, flaggedUploaders, Enumerable.Empty<IUploadBehavior>());

            const string key = "blah";
            using (var sha256 = SHA256.Create())
            {
                await trustedSources.Create(new TrustedSource
                {
                    ApiKeySha256 = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
                });
            }

            var upload = new UploadParameters();

            var result = await controller.Post(key, upload);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Controller_Post_FailsSilently_WhenFlagged()
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var controller = new UploadController(trustedSources, flaggedUploaders, Enumerable.Empty<IUploadBehavior>());

            const string key = "blah";
            const string uploaderId = "ffff";
            string uploaderIdHash;
            using (var sha256 = SHA256.Create())
            {
                await trustedSources.Create(new TrustedSource
                {
                    ApiKeySha256 = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
                });

                uploaderIdHash = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(uploaderId)));
            }

            await flaggedUploaders.Create(new FlaggedUploader { UploaderIdHash = uploaderIdHash });

            var upload = new UploadParameters { UploaderId = uploaderId };

            var result = await controller.Post(key, upload);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_Post_Validators_Run_First()
        {
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var content = new MockContentDbAccess();
            var gameData = new MockGameDataProvider();
            var worldUploadCounts = new MockWorldUploadCountDbAccess();
            var controller = new UploadController(
                trustedSources,
                flaggedUploaders,
                new List<IUploadBehavior>
                {
                    new PlayerContentUploadBehavior(content),
                    new WorldIdUploadBehavior(gameData, worldUploadCounts),
                });

            const string key = "blah";
            using (var sha256 = SHA256.Create())
            {
                await trustedSources.Create(new TrustedSource
                {
                    ApiKeySha256 = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
                });
            }

            var upload1 = new UploadParameters
            {
                WorldId = 0,
                ContentId = "afffff",
                CharacterName = "Bubu Bubba",
                UploaderId = "affffe",
            };

            var result1 = await controller.Post(key, upload1);
            Assert.IsType<NotFoundObjectResult>(result1);

            var upload2 = new UploadParameters
            {
                WorldId = 74,
                ContentId = "afffff",
                CharacterName = "Bubu Bubba",
                UploaderId = "affffe",
            };

            var result2 = await controller.Post(key, upload2);
            Assert.IsType<OkObjectResult>(result2);
        }
    }
}