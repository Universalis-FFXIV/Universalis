using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.Application.Tests.Mocks.DbAccess
{
    public class MockContentDbAccess : IContentDbAccess
    {
        private readonly Dictionary<string, Content> _collection = new();

        public Task Create(Content document)
        {
            _collection.Add(document.ContentId, document);
            return Task.CompletedTask;
        }

        public Task<Content> Retrieve(ContentQuery query)
        {
            return !_collection.TryGetValue(query.ContentId ?? "", out var taxRates)
                ? Task.FromResult<Content>(null)
                : Task.FromResult(taxRates);
        }

        public async Task Update(Content document, ContentQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public Task Delete(ContentQuery query)
        {
            _collection.Remove(query.ContentId);
            return Task.CompletedTask;
        }
    }
}