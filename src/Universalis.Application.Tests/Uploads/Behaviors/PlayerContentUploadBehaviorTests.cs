using System.Security.Cryptography;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class PlayerContentUploadBehaviorTests
{
    [Fact]
    public void Behavior_DoesNotRun_WithoutContentIdAndName()
    {
        var dbAccess = new MockCharacterDbAccess();
        var behavior = new PlayerContentUploadBehavior(dbAccess);

        var upload = new UploadParameters();

        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds()
    {
        var dbAccess = new MockCharacterDbAccess();
        var behavior = new PlayerContentUploadBehavior(dbAccess);

        var upload = new UploadParameters
        {
            ContentId = "943579483257489057",
            CharacterName = "Big Floppa",
        };

        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.Null(result);
        
        using var sha256 = SHA256.Create();
        var contentIdHash = Util.Hash(sha256, upload.ContentId);

        var data = await dbAccess.Retrieve(contentIdHash);

        Assert.NotNull(data);
        Assert.Equal(contentIdHash, data.ContentIdSha256);
        Assert.Equal(upload.CharacterName, data.Name);
        Assert.Null(data.WorldId);
    }
}