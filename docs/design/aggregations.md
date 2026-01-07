# Aggregations

Pre-computed aggregates enable sub-millisecond lookups for common queries like "cheapest price" or "recent sale velocity". All aggregates are stored in Redis.

## Why Redis?

Aggregates are stored in Redis because:

- **Sub-millisecond Latency**: Critical for API response times
- **Atomic Operations**: Safe concurrent updates
- **Data Structures**: Sorted sets perfect for rankings
- **TTL Support**: Automatic expiration of stale data

## Aggregate Types

```mermaid
flowchart TB
    subgraph Min Listings
        ML_WORLD[Per World<br/>Cheapest price]
        ML_DC[Per Datacenter<br/>Cheapest across worlds]
        ML_REGION[Per Region<br/>Cheapest across DCs]
    end

    subgraph Recent Sales
        RS_WORLD[Per World<br/>Most recent sale]
        RS_DC[Per Datacenter<br/>Most recent across worlds]
        RS_REGION[Per Region<br/>Most recent across DCs]
    end

    subgraph Trade Velocity
        TV_QTY[Quantity Sold<br/>Per day]
        TV_REV[Revenue<br/>Per day]
        TV_AVG[Avg Sales/Day<br/>Computed]
    end

    ML_WORLD --> ML_DC
    ML_DC --> ML_REGION
    RS_WORLD --> RS_DC
    RS_DC --> RS_REGION
```

## Scope Hierarchy

All aggregates support three scope levels:

```mermaid
flowchart TB
    WORLD[World Level<br/>Single server]
    DC[Datacenter Level<br/>Group of worlds]
    REGION[Region Level<br/>Group of DCs]

    WORLD --> DC
    DC --> REGION
```

Example hierarchy:

- **World**: Adamantoise
- **Datacenter**: Aether (includes Adamantoise, Cactuar, etc.)
- **Region**: North America (includes Aether, Primal, Crystal)

## MinListing

Tracks the cheapest listing for each item at each scope.

### Data Structure

```mermaid
flowchart LR
    subgraph World Level
        W_KEY["min-listing:{worldId}:{itemId}:{hq|nq}"]
        W_VAL["{ price, worldId }"]
    end

    subgraph DC/Region Level
        DC_KEY["min-listing:{dcId}:{itemId}:{hq|nq}"]
        DC_SET["Sorted Set<br/>Score = price<br/>Member = worldId"]
    end
```

- **World Level**: Simple string with price and world
- **DC/Region Level**: Sorted set to find cheapest across worlds

### Update Flow

```mermaid
flowchart TB
    UPLOAD[New Listings] --> FIND_MIN[Find Minimum Price]
    FIND_MIN --> UPDATE_WORLD[Update World Key]
    UPDATE_WORLD --> UPDATE_DC[Update DC Sorted Set]
    UPDATE_DC --> UPDATE_REGION[Update Region Sorted Set]
```

Updates are fire-and-forget (non-blocking).

## RecentSale

Tracks the most recent sale for each item at each scope.

### Data Structure

```mermaid
flowchart LR
    subgraph Per Scope
        KEY["recent-sales:{scope}:{itemId}:{hq|nq}"]
        SET["Sorted Set<br/>Score = sale_time<br/>Member = { price, worldId }"]
    end
```

Sorted sets enable:

- Find most recent sale: `ZREVRANGE ... LIMIT 1`
- Find which world had most recent: member includes worldId

## TradeVelocity

Tracks daily trading volume and revenue.

### Data Structure

```mermaid
flowchart LR
    subgraph Quantity Key
        QTY_KEY["sale-qu:{scope}:{itemId}:{hq|nq}:{dateO}"]
        QTY_VAL["Integer count"]
    end

    subgraph Revenue Key
        REV_KEY["sale-pr:{scope}:{itemId}:{hq|nq}:{dateO}"]
        REV_VAL["Integer sum"]
    end
```

- `qu` = quantity sum for the day
- `pr` = price (revenue) sum for the day
- `dateO` = date in ordinal format

### Computation

```mermaid
flowchart TB
    DAILY_QTY[Daily Quantity] --> SUM_QTY[Sum over period]
    DAILY_REV[Daily Revenue] --> SUM_REV[Sum over period]
    SUM_QTY --> VELOCITY["Velocity =<br/>Total Qty / Days"]
```

### TTL

Trade velocity keys expire after **7 days** to:

- Limit storage growth
- Keep data fresh
- Allow recalculation on query

## Update Pattern

```mermaid
sequenceDiagram
    participant Upload
    participant Processing
    participant Redis
    participant Response

    Upload->>Processing: New data
    Processing->>Response: Return success
    Note over Processing: Non-blocking
    Processing->>Redis: Fire-and-forget updates
    Note over Redis: Async aggregate updates
```

Key design decision: Aggregate updates don't block the upload response.

- Uses `CommandFlags.FireAndForget`
- Eventual consistency is acceptable
- Keeps upload latency low

## Query Patterns

### Get Minimum Price

```
GET min-listing:{worldId}:{itemId}:nq
GET min-listing:{worldId}:{itemId}:hq
```

### Get Cheapest Across Datacenter

```
ZRANGE min-listing:{dcId}:{itemId}:nq 0 0 WITHSCORES
```

### Get Trade Velocity

```
MGET sale-qu:{scope}:{itemId}:nq:{date1} sale-qu:{scope}:{itemId}:nq:{date2} ...
MGET sale-pr:{scope}:{itemId}:nq:{date1} sale-pr:{scope}:{itemId}:nq:{date2} ...
```

Then compute average.

## Redis Database Layout

Redis databases are partitioned by purpose:

| Database | Purpose                                                 |
| -------- | ------------------------------------------------------- |
| DB 0     | Upload timestamps (sorted sets by world)                |
| DB 1     | Tax rate information                                    |
| DB 2     | Current market state                                    |
| DB 3     | Aggregates (min listings, recent sales, trade velocity) |

## Consistency Model

Aggregates are **eventually consistent**:

- Updated asynchronously after uploads
- Brief windows where aggregate may lag actual data
- Acceptable tradeoff for low latency

For exact current values, query the source data directly.
