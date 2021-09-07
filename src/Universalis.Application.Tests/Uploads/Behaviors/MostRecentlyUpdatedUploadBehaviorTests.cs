using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors
{
    public class MostRecentlyUpdatedUploadBehaviorTests
    {
        [Fact]
        public void Behavior_DoesNotRun_WithoutWorldId()
        {
            var gameData = new MockGameDataProvider();
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(gameData, mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                ItemId = 5333,
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public void Behavior_DoesNotRun_InvalidWorldId()
        {
            var gameData = new MockGameDataProvider();
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(gameData, mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                ItemId = 5333,
                WorldId = 0,
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public void Behavior_DoesNotRun_WithoutItemId()
        {
            var gameData = new MockGameDataProvider();
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(gameData, mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                WorldId = 74,
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public async Task Behavior_Succeeds()
        {
            var gameData = new MockGameDataProvider();
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(gameData, mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                ItemId = 5333,
                WorldId = 74,
            };

            Assert.True(behavior.ShouldExecute(upload));

            var result = await behavior.Execute(null, upload);
            Assert.Null(result);

            var data = await mostRecentlyUpdatedDb.RetrieveMany(new MostRecentlyUpdatedManyQuery { WorldIds = new[] { 74U } });
            Assert.NotNull(data);
            Assert.Single(data);
            Assert.Equal(upload.ItemId.Value, data[0].Uploads[0].ItemId);
            Assert.Equal(upload.WorldId.Value, data[0].Uploads[0].WorldId);
            Assert.Equal(upload.WorldId.Value, data[0].WorldId);
        }
    }
}