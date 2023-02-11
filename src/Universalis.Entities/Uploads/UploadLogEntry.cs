using System;

namespace Universalis.Entities.Uploads;

public class UploadLogEntry
{
    public Guid Id { get; init; }

    public DateTime Timestamp { get; init; }

    public string Event { get; init; }

    public string Application { get; init; }

    public int WorldId { get; init; }

    public int ItemId { get; init; }

    public int Listings { get; init; }

    public int Sales { get; init; }
}