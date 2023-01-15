using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Universalis.Entities;

namespace Universalis.DbAccess;

public class CharacterStore : ICharacterStore
{
    private readonly IMapper _mapper;

    public CharacterStore(ICluster cluster)
    {
        var scylla = cluster.Connect();
        scylla.CreateKeyspaceIfNotExists("character");
        scylla.ChangeKeyspace("character");
        var table = scylla.GetTable<Character>();
        table.CreateIfNotExists();

        _mapper = new Mapper(scylla);
    }

    public Task Insert(Character character, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Insert");

        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return _mapper.InsertAsync(character);
    }
    
    public Task Update(Character character, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Update");

        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return _mapper.InsertAsync(character);
    }

    public Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Retrieve");

        if (contentIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(contentIdSha256));
        }

        return _mapper.FirstOrDefaultAsync<Character>("SELECT * FROM character WHERE content_id_sha256=?", contentIdSha256);
    }
}
