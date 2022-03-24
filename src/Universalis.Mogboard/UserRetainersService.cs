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

    public UserRetainer Get(UserRetainerId id)
    {
        throw new NotImplementedException();
    }
}