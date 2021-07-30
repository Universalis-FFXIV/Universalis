using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockTrustedSourceDbAccess : ITrustedSourceDbAccess
    {
        private readonly Dictionary<string, TrustedSource> _collection = new();

        public Task Create(TrustedSource document)
        {
            _collection.Add(document.ApiKeySha256, document);
            return Task.CompletedTask;
        }

        public Task<TrustedSource> Retrieve(TrustedSourceQuery query)
        {
            return Task.FromResult(_collection
                .FirstOrDefault(s => s.Key == query.ApiKeySha256).Value);
        }

        public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts()
        {
            return Task.FromResult(_collection.Values.AsEnumerable()
                .Select(s => new TrustedSourceNoApiKey
                {
                    Name = s.Name,
                    UploadCount = s.UploadCount,
                }));
        }

        public async Task Update(TrustedSource document, TrustedSourceQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public async Task Increment(TrustedSourceQuery query)
        {
            var document = await Retrieve(query);
            if (document == null)
            {
                return;
            }

            await Update(new TrustedSource
            {
                ApiKeySha256 = document.ApiKeySha256,
                Name = document.Name,
                UploadCount = document.UploadCount + 1,
            }, query);
        }

        public Task Delete(TrustedSourceQuery query)
        {
            _collection.Remove(query.ApiKeySha256);
            return Task.CompletedTask;
        }
    }
}