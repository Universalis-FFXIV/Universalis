using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities;

namespace Universalis.DbAccess;

public class CharacterStore : ICharacterStore
{
    private readonly string _connectionString;

    public CharacterStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Insert(Character character, CancellationToken cancellationToken = default)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO character (content_id_sha256, name, world_id) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = character.ContentIdSha256 },
                    new NpgsqlParameter<string> { TypedValue = character.Name },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(character.WorldId) },
                },
            };

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.ConstraintName == "PK_character_content_id_sha256")
        {
            // Race condition; unique constraint violated
        }
    }
    
    public async Task Update(Character character, CancellationToken cancellationToken = default)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        if (await Retrieve(character.ContentIdSha256, cancellationToken) == null)
        {
            await Insert(character, cancellationToken);
            return;
        }

        await using var command =
            new NpgsqlCommand(
                "UPDATE character SET name = $1, world_id = $2 WHERE content_id_sha256 = $3", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = character.Name },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(character.WorldId) },
                    new NpgsqlParameter<string> { TypedValue = character.ContentIdSha256 },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Character> Retrieve(string contentIdSha256, CancellationToken cancellationToken = default)
    {
        if (contentIdSha256 == null)
        {
            throw new ArgumentNullException(nameof(contentIdSha256));
        }
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT content_id_sha256, name, world_id FROM character WHERE content_id_sha256 = $1", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = contentIdSha256 },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        var contentIdHash = reader.GetString(0);
        var name = reader.GetString(1);
        var worldId = Convert.ToUInt32(reader.GetInt32(2));
        return new Character(contentIdHash, name, worldId);
    }
}
