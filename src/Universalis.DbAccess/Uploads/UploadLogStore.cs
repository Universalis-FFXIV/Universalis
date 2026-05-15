using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class UploadLogStore : IUploadLogStore
{
    private const string InsertSql =
        "INSERT INTO upload_log (id, timestamp, event, application, world_id, item_id, listings, sales, user_agent) " +
        "VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)";

    private readonly NpgsqlDataSource _dataSource;

    public UploadLogStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task LogAction(UploadLogEntry entry)
    {
        using var activity = Util.ActivitySource.StartActivity("UploadLogStore.LogAction");
        await using var command = _dataSource.CreateCommand(InsertSql);
        AddRowParameters(command.Parameters, entry);
        await command.ExecuteNonQueryAsync();
    }

    public async Task LogActions(IReadOnlyCollection<UploadLogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        using var activity = Util.ActivitySource.StartActivity("UploadLogStore.LogActions");
        activity?.AddTag("batchSize", entries.Count);

        // NpgsqlBatch pipelines the inserts in a single round-trip and wraps
        // them in an implicit transaction, so a failure mid-batch rolls back
        // the whole batch rather than leaving partial audit rows behind.
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection);
        foreach (var entry in entries)
        {
            var cmd = new NpgsqlBatchCommand(InsertSql);
            AddRowParameters(cmd.Parameters, entry);
            batch.BatchCommands.Add(cmd);
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRowParameters(NpgsqlParameterCollection parameters, UploadLogEntry entry)
    {
        parameters.Add(new NpgsqlParameter<Guid> { TypedValue = entry.Id });
        parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = entry.Timestamp });
        parameters.Add(new NpgsqlParameter<string> { TypedValue = entry.Event });
        parameters.Add(new NpgsqlParameter<string> { TypedValue = entry.Application });
        parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.WorldId });
        parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.ItemId });
        parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.Listings });
        parameters.Add(new NpgsqlParameter<int> { TypedValue = entry.Sales });
        parameters.Add(new NpgsqlParameter { Value = (object)entry.UserAgent ?? DBNull.Value });
    }
}