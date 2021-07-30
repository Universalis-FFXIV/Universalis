using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockWorldUploadCountDbAccess : IWorldUploadCountDbAccess
    {
        private readonly Dictionary<string, WorldUploadCount> _collection = new();

        public Task Create(WorldUploadCount document)
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

        public Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts()
        {
            return Task.FromResult(_collection.Values.AsEnumerable());
        }

        public async Task Update(WorldUploadCount document, WorldUploadCountQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public async Task Increment(WorldUploadCountQuery query)
        {
            var document = await Retrieve(query);
            await Update(new WorldUploadCount
            {
                WorldName = document?.WorldName ?? query.WorldName,
                Count = document?.Count + 1 ?? 1,
            }, query);
        }

        public Task Delete(WorldUploadCountQuery query)
        {
            _collection.Remove(query.WorldName);
            return Task.CompletedTask;
        }
    }
}