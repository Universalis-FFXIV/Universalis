using MySqlConnector;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

internal class UserListsService : IMogboardTable<UserList, UserListId>
{
    private readonly string _connectionString;

    public UserListsService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<UserList?> Get(UserListId id, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        command.CommandText = "select * from dalamud.users_lists where id=@Id limit 1;";
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = id.ToString();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? UserList.FromReader(reader) : null;
    }

    public async Task Create(UserList entity, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        entity.IntoCommand(command, "dalamud.users_lists");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}