using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess
{
    public class MockUploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
    {
        private readonly List<UploadCountHistory> _collection = new();

        public Task Create(UploadCountHistory document)
        {
            _collection.Add(document);
            return Task.CompletedTask;
        }

        public Task<UploadCountHistory> Retrieve(UploadCountHistoryQuery query)
        {
            return Task.FromResult(_collection.FirstOrDefault());
        }

        public async Task Update(UploadCountHistory document, UploadCountHistoryQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public async Task Update(uint lastPush, List<uint> uploadCountByDay)
        {
            var existing = await Retrieve(new UploadCountHistoryQuery());
            if (existing != null)
            {
                existing.LastPush = lastPush;
                existing.UploadCountByDay = uploadCountByDay;
                return;
            }

            await Create(new UploadCountHistory
            {
                LastPush = lastPush,
                UploadCountByDay = uploadCountByDay,
            });
        }

        public Task Delete(UploadCountHistoryQuery query)
        {
            _collection.Remove(_collection.FirstOrDefault());
            return Task.CompletedTask;
        }
    }
}