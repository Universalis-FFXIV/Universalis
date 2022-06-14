using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class FlaggedUploaderStore : IFlaggedUploaderStore
{
    private readonly string _connectionString;

    public FlaggedUploaderStore(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task Insert(FlaggedUploader uploader, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO flagged_uploader (id_sha256) VALUES ($1)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = uploader.IdSha256 },
                },
            };

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.ConstraintName == "PK_flagged_uploader_id_sha256")
        {
            // Race condition; unique constraint violated
        }
    }

    public async Task<FlaggedUploader> Retrieve(string uploaderIdSha256, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        await using var command =
            new NpgsqlCommand(
                "SELECT id_sha256 FROM flagged_uploader WHERE id_sha256 = $1", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<string> { TypedValue = uploaderIdSha256 },
                },
            };
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        var uploaderIdHash = reader.GetString(0);
        return new FlaggedUploader(uploaderIdHash);
    }
}