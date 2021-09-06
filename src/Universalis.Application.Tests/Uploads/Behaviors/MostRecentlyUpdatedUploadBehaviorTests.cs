using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
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
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                ItemId = 5333,
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public void Behavior_DoesNotRun_WithoutItemId()
        {
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                WorldId = 74,
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public async Task Behavior_Succeeds()
        {
            var mostRecentlyUpdatedDb = new MockMostRecentlyUpdatedDbAccess();
            var behavior = new MostRecentlyUpdatedUploadBehavior(mostRecentlyUpdatedDb);

            var upload = new UploadParameters
            {
                ItemId = 5333,
                WorldId = 74,
            };

            Assert.True(behavior.ShouldExecute(upload));

            var result = await behavior.Execute(null, upload);
            Assert.Null(result);

            var data = await mostRecentlyUpdatedDb.RetrieveMany();
            Assert.NotNull(data);
            Assert.Single(data);
            Assert.Equal(upload.ItemId.Value, data[0].ItemId);
            Assert.Equal(upload.WorldId.Value, data[0].WorldId);
        }
    }
}