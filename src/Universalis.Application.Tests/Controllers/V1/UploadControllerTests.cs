using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
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
                    ApiKeySha256 = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
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
                    ApiKeySha256 = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
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
                    ApiKeySha256 = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))),
                });

                uploaderIdHash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(uploaderId)));
            }

            await flaggedUploaders.Create(new FlaggedUploader { UploaderIdHash = uploaderIdHash });

            var upload = new UploadParameters { UploaderId = uploaderId };

            var result = await controller.Post(key, upload);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}