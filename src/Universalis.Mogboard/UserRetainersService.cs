using MySqlConnector;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

public class UserRetainersService : IMogboardTable<UserRetainer, UserRetainerId>
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _database;
    private readonly int _port;

    public UserRetainersService(string username, string password, string database, int port)
    {
        _username = username;
        _password = password;
        _database = database;
        _port = port;
    }

    public async Task<UserRetainer?> Get(UserRetainerId id)
    {
        await using var db = new MySqlConnection($"User ID={_username};Password={_password};Database={_database};Port={_port}");
        db.Open();

        await using var command = db.CreateCommand();
        command.CommandText = "select * from dalamud.users_retainers where id=@id limit 1;";
        command.Parameters.Add("@id", MySqlDbType.VarChar);
        command.Parameters["@id"].Value = id.ToString();

        await using var reader = await command.ExecuteReaderAsync();
        return reader.Read() ? UserRetainer.FromReader(reader) : null;
    }
}