# Sales Data Flow

Historical sales represent completed transactions. This document covers storage, querying, and the request deduplication pattern.

## Why ScyllaDB?

Sales are stored in ScyllaDB (Cassandra-compatible) because:

- **Time-Series Data**: Sales are append-only, ordered by time
- **High Write Throughput**: Can handle massive upload volume
- **Horizontal Scaling**: Add nodes to increase capacity
- **Partition Strategy**: Efficient queries by item/world

## Sale Entity

```mermaid
erDiagram
    SALE {
        uuid id PK
        int item_id "Partition Key"
        int world_id "Partition Key"
        timestamp sale_time "Clustering Key DESC"
        int price_per_unit
        int quantity "nullable"
        string buyer_name "nullable"
        bool hq
        bool on_mannequin "nullable"
        string uploader_id_hash
    }
```

### Partitioning Strategy

```mermaid
flowchart TB
    subgraph Partition Key
        ITEM[item_id]
        WORLD[world_id]
    end

    subgraph Clustering
        TIME[sale_time DESC]
    end

    ITEM --> PARTITION[Partition]
    WORLD --> PARTITION
    PARTITION --> TIME
    TIME --> DATA[Sale Records]
```

- **Partition Key**: `(item_id, world_id)` - all sales for an item on a world are co-located
- **Clustering Key**: `sale_time DESC` - newest sales first within partition
- Enables efficient "most recent N sales" queries

### Nullable Fields

Some fields were added after initial launch:

- `quantity` - Added later, older records may be null
- `buyer_name` - Added May 2022
- `on_mannequin` - Added June 2022

## Write Path

```mermaid
flowchart TB
    subgraph Upload Processing
        UPLOAD[Upload Request] --> CLEAN[Clean Sales]
        CLEAN --> VALIDATE[Validate Timestamps]
        VALIDATE --> FILTER[Filter Future Dates]
    end

    subgraph Deduplication
        FILTER --> FETCH[Fetch Existing History]
        FETCH --> FINGERPRINT[SHA-256 Fingerprint]
        FINGERPRINT --> UNIQUE[Keep Unique Only]
    end

    subgraph Storage
        UNIQUE --> BATCH[Batch Insert]
        BATCH --> SCYLLA[(ScyllaDB)]
    end

    subgraph Events
        UNIQUE --> PUBLISH[Publish SalesAdd]
    end
```

### Sale Fingerprinting

Sales are deduplicated using a fingerprint:

```
SHA-256(item_id + world_id + price + quantity + sale_time + buyer + hq)
```

This prevents duplicate sales from multiple uploaders seeing the same transaction.

## Read Path

```mermaid
flowchart TB
    subgraph Query
        REQ[API Request] --> PARSE[Parse Parameters]
        PARSE --> KEY[Generate Request Key]
    end

    subgraph Deduplication
        KEY --> CHECK{Pending<br/>Request?}
        CHECK -->|Yes| JOIN[Join Existing Task]
        CHECK -->|No| THROTTLE[Acquire Semaphore]
    end

    subgraph Database
        THROTTLE --> SCYLLA[(ScyllaDB)]
        SCYLLA --> RESULTS[Results]
    end

    subgraph Response
        JOIN --> WAIT[Await Result]
        RESULTS --> STORE[Store in Pending]
        STORE --> RETURN[Return Results]
        WAIT --> RETURN
    end
```

### Request Deduplication Pattern

A key optimization for handling load spikes:

```mermaid
sequenceDiagram
    participant Client1
    participant Client2
    participant Dedup
    participant Database

    Client1->>Dedup: Request (world=1, item=5)
    Note over Dedup: Create pending task
    Dedup->>Database: Query
    Client2->>Dedup: Request (world=1, item=5)
    Note over Dedup: Same key exists!
    Dedup-->>Client2: Join existing task
    Database-->>Dedup: Results
    Dedup-->>Client1: Results
    Dedup-->>Client2: Results (same data)
```

The deduplication key includes:

- `worldId`
- `itemId`
- `count` (number of entries)
- `from` (time range start, if specified)
- `to` (time range end, if specified)

### Throttling

A semaphore limits concurrent ScyllaDB requests to prevent overload:

- Prevents thundering herd scenarios
- Configurable concurrency limit
- Works with deduplication for maximum efficiency

## Query Patterns

### Recent Sales

```cql
SELECT * FROM sale
WHERE item_id = ? AND world_id = ?
ORDER BY sale_time DESC
LIMIT ?
```

### Time-Filtered Sales

```cql
SELECT * FROM sale
WHERE item_id = ? AND world_id = ?
  AND sale_time >= ? AND sale_time <= ?
ORDER BY sale_time DESC
```

## Pagination

For large result sets:

- Configurable page size (default: 100)
- ScyllaDB handles pagination automatically
- Prevents memory exhaustion on massive histories

## Connection Configuration

| Setting                     | Value              | Purpose                       |
| --------------------------- | ------------------ | ----------------------------- |
| Max requests per connection | 3000               | Prevent connection saturation |
| Query timeout               | 5000ms             | Fail fast on slow queries     |
| Speculative execution       | 3 attempts @ 400ms | Handle tail latencies         |
| Default idempotence         | true               | Safe retries                  |

## Metrics

| Metric                       | Description                |
| ---------------------------- | -------------------------- |
| `universalis_sale_rows_read` | Histogram of rows returned |
| Query latency                | Via OpenTelemetry tracing  |
