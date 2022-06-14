using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess;
using Universalis.Entities;

namespace Universalis.Application.Tests.Mocks.DbAccess;

public class MockCharacterDbAccess : ICharacterDbAccess
{
    private readonly Dictionary<string, Character> _data = new();

    public Task Create(Character character, CancellationToken cancellationToken = default)
    {
        _data[character.ContentIdSha256] = character;
        return Task.CompletedTask;
    }

    public Task Update(Character character, CancellationToken cancellationToken = default)
    {
        if (_data.ContainsKey(character.ContentIdSha256))
        {
            _data[character.ContentIdSha256] = character;
        }
        
        return Task.CompletedTask;
    }

    public Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        return !_data.ContainsKey(contentIdSha256)
            ? Task.FromResult<Character>(null)
            : Task.FromResult(_data[contentIdSha256]);
    }
}