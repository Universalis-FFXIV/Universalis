using MySqlConnector;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

internal class UserAlertEventsService : IMogboardTable<UserAlertEvent, UserAlertEventId>
{
    private readonly string _connectionString;

    public UserAlertEventsService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<UserAlertEvent?> Get(UserAlertEventId id, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        command.CommandText = "select * from dalamud.users_alerts_events where id=@Id limit 1;";
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = id.ToString();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? UserAlertEvent.FromReader(reader) : null;
    }

    public async Task Create(UserAlertEvent entity, CancellationToken cancellationToken = default)
    {
        await using var db = new MySqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        await using var command = db.CreateCommand();
        entity.IntoCommand(command, "dalamud.users_alerts_events");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}