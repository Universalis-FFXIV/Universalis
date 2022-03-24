using MySqlConnector;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

public class UserSessionsService : IMogboardTable<UserSession, UserSessionId>
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _database;
    private readonly int _port;

    public UserSessionsService(string username, string password, string database, int port)
    {
        _username = username;
        _password = password;
        _database = database;
        _port = port;
    }

    public UserSession? Get(UserSessionId id)
    {
        using var db = new MySqlConnection($"User ID={_username};Password={_password};Database={_database};Port={_port}");
        db.Open();

        using var command = db.CreateCommand();
        command.CommandText = "select * from dalamud.users_sessions where id=@id limit 1;";
        command.Parameters.Add("@id", MySqlDbType.VarChar);
        command.Parameters["@id"].Value = id.ToString();

        using var reader = command.ExecuteReader();
        return reader.Read() ? UserSession.FromReader(reader) : null;
    }
}