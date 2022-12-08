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
    private readonly IMapper _mapper;

    public ApiKeyStore(ICluster cluster)
    {
        var scylla = cluster.Connect();
        scylla.CreateKeyspaceIfNotExists("api_key");
        scylla.ChangeKeyspace("api_key");
        var table = scylla.GetTable<ApiKey>();
        table.CreateIfNotExists();

        _mapper = new Mapper(scylla);
    }

    public Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        if (apiKey == null)
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        return _mapper.InsertAsync(apiKey);
    }

    public Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default)
    {
        if (tokenSha512 == null)
        {
            throw new ArgumentNullException(nameof(tokenSha512));
        }

        return _mapper.FirstOrDefaultAsync<ApiKey>("SELECT * FROM api_key WHERE token_sha512=?", tokenSha512);
    }
}