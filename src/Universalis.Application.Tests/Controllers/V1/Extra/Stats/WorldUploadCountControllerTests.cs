using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess;
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
            Assert.True(counts[query.WorldName].Count == 1);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            Assert.True(counts[query.WorldName].Proportion == 1);
        }
    }
}