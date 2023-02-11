using System;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class UploadLogStore : IUploadLogStore
{
    private readonly NpgsqlDataSource _dataSource;

    public UploadLogStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task LogAction(UploadLogEntry entry)
    {
        using var activity = Util.ActivitySource.StartActivity("UploadLogStore.LogAction");
        await using var command = _dataSource.CreateCommand(
            "INSERT INTO upload_log (id, timestamp, event, application, world_id, item_id, listings, sales) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
        command.Parameters.Add(new NpgsqlParameter<Guid> { TypedValue = entry.Id });
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset> { TypedValue = entry.Timestamp });
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = entry.Event });
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = entry.Application });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.WorldId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.Listings });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.Sales });
        await command.ExecuteNonQueryAsync();
    }
}