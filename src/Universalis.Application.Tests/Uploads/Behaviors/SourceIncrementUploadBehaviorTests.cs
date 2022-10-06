using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.Entities.AccessControl;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class SourceIncrementUploadBehaviorTests
{
    public class PlayerContentUploadBehaviorTests
    {
        [Fact]
        public async Task Behavior_Succeeds()
        {
            var dbAccess = new MockTrustedSourceDbAccess();
            var behavior = new SourceIncrementUploadBehavior(dbAccess);

            const string key = "blah";
            string keyHash;
            using (var sha256 = SHA256.Create())
            {
                await using var keyStream = new MemoryStream(Encoding.UTF8.GetBytes(key));
                keyHash = Util.BytesToString(await sha256.ComputeHashAsync(keyStream));
            }

            var source = new ApiKey(keyHash, "something", true);
            await dbAccess.Create(source);

            var upload = new UploadParameters();
            Assert.True(behavior.ShouldExecute(upload));

            var result = await behavior.Execute(source, upload);
            Assert.Null(result);

            var data = await dbAccess.GetUploaderCounts();

            Assert.NotNull(data);
            Assert.Equal(1, data.First().UploadCount);
        }
    }
}