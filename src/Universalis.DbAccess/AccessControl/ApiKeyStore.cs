using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public class ApiKeyStore : IApiKeyStore
{
    private readonly Lazy<IMapper> _mapper;

    public ApiKeyStore(ICluster scylla)
    {
        _mapper = new Lazy<IMapper>(() =>
        {
            var db = scylla.Connect();
            db.CreateKeyspaceIfNotExists("api_key");
            db.ChangeKeyspace("api_key");
            var table = db.GetTable<ApiKey>();
            table.CreateIfNotExists();
            return new Mapper(db);
        });
    }

    public Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ApiKeyStore.Insert");
        if (apiKey == null)
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        return _mapper.Value.InsertAsync(apiKey);
    }

    public Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ApiKeyStore.Retrieve");

        if (tokenSha512 == null)
        {
            throw new ArgumentNullException(nameof(tokenSha512));
        }

        return _mapper.Value.FirstOrDefaultAsync<ApiKey>("SELECT * FROM api_key WHERE token_sha512=?", tokenSha512);
    }
}