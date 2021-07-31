using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries;
using Universalis.Entities;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors
{
    public class PlayerContentUploadBehaviorTests
    {
        [Fact]
        public async Task Behavior_DoesNotRun_WithoutContentIdAndName()
        {
            var dbAccess = new MockContentDbAccess();
            var behavior = new PlayerContentUploadBehavior(dbAccess);

            var upload = new UploadParameters();

            Assert.False(behavior.ShouldExecute(upload));

            var data = await dbAccess.Retrieve(new ContentQuery
            {
                ContentId = upload.ContentId,
            });

            Assert.Null(data);
        }

        [Fact]
        public async Task Behavior_Succeeds()
        {
            var dbAccess = new MockContentDbAccess();
            var behavior = new PlayerContentUploadBehavior(dbAccess);

            var upload = new UploadParameters
            {
                ContentId = "943579483257489057",
                CharacterName = "Big Floppa",
            };

            Assert.True(behavior.ShouldExecute(upload));

            var result = await behavior.Execute(null, upload);
            Assert.Null(result);

            var data = await dbAccess.Retrieve(new ContentQuery
            {
                ContentId = upload.ContentId,
            });

            Assert.NotNull(data);
            Assert.Equal(upload.ContentId, data.ContentId);
            Assert.Equal(upload.CharacterName, data.CharacterName);
            Assert.Equal(ContentKind.Player, data.ContentType);
        }
    }
}