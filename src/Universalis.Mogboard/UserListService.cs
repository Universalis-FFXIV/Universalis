using MySqlConnector;
using Universalis.Mogboard.Entities;

namespace Universalis.Mogboard;

public class UserListService
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _database;

    public UserListService(string username, string password, string database)
    {
        _username = username;
        _password = password;
        _database = database;
    }

    public UserList? Get(string id)
    {
        using var db = new MySqlConnection($"User ID={_username};Password={_password};Database={_database}");
        db.Open();

        using var command = new MySqlCommand("select * from dalamud.users_lists where id='@id' limit 1;", db);
        command.Parameters.AddWithValue("id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? reader.ConvertToObject<UserList>() : null;
    }
}