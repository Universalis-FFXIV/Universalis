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
    private readonly Lazy<IMapper> _mapper;

    public CharacterStore(ICluster scylla)
    {
        _mapper = new Lazy<IMapper>(() =>
        {
            var db = scylla.Connect();
            db.CreateKeyspaceIfNotExists("character");
            db.ChangeKeyspace("character");
            var table = db.GetTable<Character>();
            table.CreateIfNotExists();
            return new Mapper(db);
        });
    }

    public Task Insert(Character character, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Insert");

        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return _mapper.Value.InsertAsync(character);
    }
    
    public Task Update(Character character, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Update");

        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return _mapper.Value.InsertAsync(character);
    }

    public Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CharacterStore.Retrieve");

        if (contentIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(contentIdSha256));
        }

        return _mapper.Value.FirstOrDefaultAsync<Character>("SELECT * FROM character WHERE content_id_sha256=?", contentIdSha256);
    }
}
