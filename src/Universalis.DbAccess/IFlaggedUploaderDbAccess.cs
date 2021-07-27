using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public interface IFlaggedUploaderDbAccess
    {
        public Task Create(FlaggedUploader document);

        public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query);

        public Task Update(FlaggedUploader document, FlaggedUploaderQuery query);

        public Task Delete(FlaggedUploaderQuery query);
    }
}