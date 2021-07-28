using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IFlaggedUploaderDbAccess
    {
        public Task Create(FlaggedUploader document);

        public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query);

        public Task Update(FlaggedUploader document, FlaggedUploaderQuery query);

        public Task Delete(FlaggedUploaderQuery query);
    }
}