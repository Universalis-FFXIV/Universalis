using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockWorldUploadCountDbAccess : IWorldUploadCountDbAccess
    {
        private readonly Dictionary<string, WorldUploadCount> _collection = new();

        public Task Create(WorldUploadCount document, CancellationToken cancellationToken = default)
        {
            _collection.Add(document.WorldName, document);
            return Task.CompletedTask;
        }

        public Task<WorldUploadCount> Retrieve(WorldUploadCountQuery query)
        {
            return !_collection.TryGetValue(query.WorldName, out var taxRates)
                ? Task.FromResult<WorldUploadCount>(null)
                : Task.FromResult(taxRates);
        }

        public Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_collection.Values.AsEnumerable());
        }

        public async Task Update(WorldUploadCount document, WorldUploadCountQuery query, CancellationToken cancellationToken = default)
        {
            await Delete(query, cancellationToken);
            await Create(document, cancellationToken);
        }

        public async Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
        {
            var document = await Retrieve(query);
            await Update(new WorldUploadCount
            {
                WorldName = document?.WorldName ?? query.WorldName,
                Count = document?.Count + 1 ?? 1,
            }, query, cancellationToken);
        }

        public Task Delete(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
        {
            _collection.Remove(query.WorldName);
            return Task.CompletedTask;
        }
    }
}