using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Views;
using Universalis.DbAccess.Queries.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats
{
    public class WorldUploadCountControllerTests
    {
        [Fact]
        public async Task Controller_Get_Succeeds()
        {
            var dbAccess = new MockWorldUploadCountDbAccess();
            var controller = new WorldUploadCountController(dbAccess);

            var query = new WorldUploadCountQuery
            {
                WorldName = "Coeurl",
            };

            await dbAccess.Increment(query);

            var result = await controller.Get();
            var counts = Assert.IsAssignableFrom<IDictionary<string, WorldUploadCountView>>(result);

            Assert.True(counts.ContainsKey(query.WorldName));
            Assert.Equal(1U, counts[query.WorldName].Count);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            Assert.Equal(1, counts[query.WorldName].Proportion);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_WhenNone()
        {
            var dbAccess = new MockWorldUploadCountDbAccess();
            var controller = new WorldUploadCountController(dbAccess);

            var result = await controller.Get();
            var counts = Assert.IsAssignableFrom<IDictionary<string, WorldUploadCountView>>(result);

            Assert.Equal(0, counts.Count);
        }
    }
}