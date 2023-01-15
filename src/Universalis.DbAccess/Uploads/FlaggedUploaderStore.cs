using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class FlaggedUploaderStore : IFlaggedUploaderStore
{
    private readonly Lazy<IMapper> _mapper;

    public FlaggedUploaderStore(ICluster scylla)
    {
        _mapper = new Lazy<IMapper>(() =>
        {
            var db = scylla.Connect();
            db.CreateKeyspaceIfNotExists("flagged_uploader");
            db.ChangeKeyspace("flagged_uploader");
            var table = db.GetTable<FlaggedUploader>();
            table.CreateIfNotExists();
            return new Mapper(db);
        });
    }

    public Task Insert(FlaggedUploader uploader, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("FlaggedUploaderStore.Insert");

        if (uploader == null)
        {
            throw new ArgumentNullException(nameof(uploader));
        }

        return _mapper.Value.InsertAsync(uploader);
    }

    public Task<FlaggedUploader> Retrieve(string uploaderIdSha256, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("FlaggedUploaderStore.Retrieve");

        if (uploaderIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(uploaderIdSha256));
        }

        return _mapper.Value.FirstOrDefaultAsync<FlaggedUploader>("SELECT * FROM flagged_uploader WHERE id_sha256=?", uploaderIdSha256);
    }
}