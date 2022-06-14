using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities;

namespace Universalis.DbAccess;

public interface ICharacterDbAccess
{
    Task Create(Character character, CancellationToken cancellationToken = default);

    Task Update(Character character, CancellationToken cancellationToken = default);

    Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default);
}