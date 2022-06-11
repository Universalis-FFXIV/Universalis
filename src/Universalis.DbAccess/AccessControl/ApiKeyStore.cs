using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public class ApiKeyStore : IApiKeyStore
{
    private readonly string _connectionString;

    public ApiKeyStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO api_key (token_sha512, name, can_upload) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = apiKey.TokenSha512 },
                    new NpgsqlParameter { Value = apiKey.Name },
                    new NpgsqlParameter { Value = apiKey.CanUpload },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT name, can_upload FROM api_key WHERE token_sha512 = $1", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = tokenSha512 },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        var name = reader.GetString(0);
        var canUpload = reader.GetBoolean(1);
        return new ApiKey(tokenSha512, name, canUpload);
    }
}