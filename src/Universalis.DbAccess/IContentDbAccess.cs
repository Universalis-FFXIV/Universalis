using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.DbAccess
{
    public interface IContentDbAccess
    {
        public Task Create(Content document);

        public Task<Content> Retrieve(ContentQuery query);

        public Task Update(Content document, ContentQuery query);

        public Task Delete(ContentQuery query);
    }
}