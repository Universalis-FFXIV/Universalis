using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockMostRecentlyUpdatedDbAccess : IMostRecentlyUpdatedDbAccess
    {
        private readonly List<WorldItemUpload> _store = new();

        public Task Create(WorldItemUpload document, CancellationToken cancellationToken = default)
        {
            if (_store.Any())
                _store.RemoveAt(0);
            _store.Add(document);
            return Task.CompletedTask;
        }

        public Task<WorldItemUpload> Retrieve(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.FirstOrDefault());
        }

        public async Task<IList<WorldItemUpload>> RetrieveMany(int? count = null, CancellationToken cancellationToken = default)
        {
            return count.HasValue ? _store.Take(count.Value).ToList() : _store;
        }
    }
}