using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class FlaggedUploaderDbAccessTests
{
    private class MockFlaggedUploaderStore : IFlaggedUploaderStore
    {
        private readonly Dictionary<string, FlaggedUploader> _data = new();
        
        public Task Insert(FlaggedUploader uploader, CancellationToken cancellationToken = default)
        {
            _data[uploader.IdSha256] = uploader;
            return Task.CompletedTask;
        }

        public Task<FlaggedUploader> Retrieve(string uploaderIdSha256, CancellationToken cancellationToken = default)
        {
            return _data.ContainsKey(uploaderIdSha256)
                ? Task.FromResult(_data[uploaderIdSha256])
                : Task.FromResult<FlaggedUploader>(null);
        }
    }
    
    [Fact]
    public async Task Create_DoesNotThrow()
    {
        var db = new FlaggedUploaderDbAccess(new MockFlaggedUploaderStore());
        var document = SeedDataGenerator.MakeFlaggedUploader();
        await db.Create(document);
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        var db = new FlaggedUploaderDbAccess(new MockFlaggedUploaderStore());
        var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdSha256 = "affffe" });
        Assert.Null(output);
    }

    [Fact]
    public async Task Create_DoesInsert()
    {
        var db = new FlaggedUploaderDbAccess(new MockFlaggedUploaderStore());
        var document = SeedDataGenerator.MakeFlaggedUploader();
        await db.Create(document);

        var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdSha256 = document.IdSha256 });
        Assert.NotNull(output);
    }
}