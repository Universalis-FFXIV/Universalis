using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public interface IApiKeyStore
{
    Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default);

    Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default);
}