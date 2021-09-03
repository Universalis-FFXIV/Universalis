using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockTrustedSourceDbAccess : ITrustedSourceDbAccess
    {
        private readonly Dictionary<string, TrustedSource> _collection = new();

        public Task Create(TrustedSource document, CancellationToken cancellationToken = default)
        {
            _collection.Add(document.ApiKeySha512, document);
            return Task.CompletedTask;
        }

        public Task<TrustedSource> Retrieve(TrustedSourceQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_collection
                .FirstOrDefault(s => s.Key == query.ApiKeySha512).Value);
        }

        public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_collection.Values.AsEnumerable()
                .Select(s => new TrustedSourceNoApiKey
                {
                    Name = s.Name,
                    UploadCount = s.UploadCount,
                }));
        }

        public async Task Update(TrustedSource document, TrustedSourceQuery query, CancellationToken cancellationToken = default)
        {
            await Delete(query, cancellationToken);
            await Create(document, cancellationToken);
        }

        public async Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default)
        {
            var document = await Retrieve(query, cancellationToken);
            if (document == null)
            {
                return;
            }

            await Update(new TrustedSource
            {
                ApiKeySha512 = document.ApiKeySha512,
                Name = document.Name,
                UploadCount = document.UploadCount + 1,
            }, query, cancellationToken);
        }

        public Task Delete(TrustedSourceQuery query, CancellationToken cancellationToken = default)
        {
            _collection.Remove(query.ApiKeySha512);
            return Task.CompletedTask;
        }
    }
}