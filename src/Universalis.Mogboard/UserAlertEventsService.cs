using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

public class UserAlertEventsService : IMogboardTable<UserAlertEvent, UserAlertEventId>
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _database;
    private readonly int _port;

    public UserAlertEventsService(string username, string password, string database, int port)
    {
        _username = username;
        _password = password;
        _database = database;
        _port = port;
    }

    public UserAlertEvent Get(UserAlertEventId id)
    {
        throw new NotImplementedException();
    }
}