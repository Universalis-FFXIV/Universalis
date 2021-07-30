using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockFlaggedUploaderDbAccess : IFlaggedUploaderDbAccess
    {
        private readonly Dictionary<string, FlaggedUploader> _collection = new();

        public Task Create(FlaggedUploader document)
        {
            _collection.Add(document.UploaderId, document);
            return Task.CompletedTask;
        }

        public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query)
        {
            return Task.FromResult(_collection
                .FirstOrDefault(s => s.Key == query.UploaderId).Value);
        }

        public async Task Update(FlaggedUploader document, FlaggedUploaderQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public Task Delete(FlaggedUploaderQuery query)
        {
            _collection.Remove(query.UploaderId);
            return Task.CompletedTask;
        }
    }
}