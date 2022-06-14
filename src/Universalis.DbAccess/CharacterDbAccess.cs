using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities;

namespace Universalis.DbAccess;

public class CharacterDbAccess : ICharacterDbAccess
{
    private readonly ICharacterStore _characters;

    public CharacterDbAccess(ICharacterStore characters)
    {
        _characters = characters;
    }
    
    public Task Create(Character character, CancellationToken cancellationToken = default)
    {
        return _characters.Insert(character, cancellationToken);
    }

    public Task Update(Character character, CancellationToken cancellationToken = default)
    {
        return _characters.Update(character, cancellationToken);
    }

    public Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        return _characters.Retrieve(contentIdSha256, cancellationToken);
    }
}