using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class UploadCountHistoryDbAccess : DbAccessService<UploadCountHistory, UploadCountHistoryQuery>, IUploadCountHistoryDbAccess
    {
        public UploadCountHistoryDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "extraData") { }

        public UploadCountHistoryDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "content") { }

        public Task Update(double lastPush, List<double> uploadCountByDay)
        {
            var filterBuilder = Builders<UploadCountHistory>.Filter;
            var filter = filterBuilder.Eq(o => o.SetName, UploadCountHistory.DefaultSetName);

            var updateBuilder = Builders<UploadCountHistory>.Update;
            var update1 = updateBuilder.Set(o => o.LastPush, lastPush);
            var update2 = updateBuilder.Set(o => o.UploadCountByDay, uploadCountByDay);

            return Collection.UpdateOneAsync(filter, updateBuilder.Combine(update1, update2));
        }
    }
}