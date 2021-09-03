using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.DbAccess
{
    public interface IContentDbAccess
    {
        public Task Create(Content document, CancellationToken cancellationToken = default);

        public Task<Content> Retrieve(ContentQuery query, CancellationToken cancellationToken = default);

        public Task Update(Content document, ContentQuery query, CancellationToken cancellationToken = default);

        public Task Delete(ContentQuery query, CancellationToken cancellationToken = default);
    }
}