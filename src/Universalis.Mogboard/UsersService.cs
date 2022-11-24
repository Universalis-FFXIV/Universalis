using MySqlConnector;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

internal class UsersService : IMogboardTable<User, UserId>
{
    private readonly string _connectionString;

    public UsersService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> Get(UserId id, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        command.CommandText = "select * from dalamud.users where id=@Id limit 1;";
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = id.ToString();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? User.FromReader(reader) : null;
    }

    public async Task Create(User entity, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        entity.IntoCommand(command, "dalamud.users");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}