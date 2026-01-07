# Listings Data Flow

Current market listings represent items actively for sale on retainers. This document covers the complete flow from upload to query.

## Why PostgreSQL?

Listings are stored in PostgreSQL because:

- **ACID Compliance**: Listings need consistent state (all-or-nothing updates)
- **Current State**: Only the latest listings matter, not history
- **Fast Lookups**: Indexed queries by world/item are efficient
- **Full Replacement**: Atomic upsert on each upload

## Listing Entity

```mermaid
erDiagram
    LISTING {
        string listing_id PK
        int item_id
        int world_id
        bool hq
        bool on_mannequin
        jsonb materia
        int unit_price
        int quantity
        int dye_id
        string creator_name
        string retainer_id
        string retainer_name
        int retainer_city_id
        timestamp uploaded_at
        string source
    }

    MARKET_ITEM {
        int item_id PK
        int world_id PK
        timestamp last_upload_time
    }

    LISTING }o--|| MARKET_ITEM : "belongs to"
```

Key fields:

- **listing_id**: Unique identifier (generated if missing via SHA-256)
- **materia**: Array of slot/materia pairs stored as JSONB
- **source**: Upload source for analytics
- **uploaded_at**: When this listing was last seen

## Write Path

```mermaid
flowchart TB
    subgraph Upload Processing
        UPLOAD[Upload Request] --> CLEAN[Clean Listings]
        CLEAN --> GEN_ID{Has ID?}
        GEN_ID -->|No| HASH[Generate SHA-256 ID]
        GEN_ID -->|Yes| VALIDATE
        HASH --> VALIDATE[Validate Data]
    end

    subgraph Delta Computation
        VALIDATE --> FETCH[Fetch Existing<br/>from PostgreSQL]
        FETCH --> DIFF[Compute Delta]
        DIFF --> ADDED[Added Listings]
        DIFF --> REMOVED[Removed Listings]
    end

    subgraph Storage
        ADDED --> UPSERT[PostgreSQL UPSERT]
        REMOVED --> DELETE[Mark Removed]
        UPSERT --> CACHE_INV[Invalidate Cache]
        DELETE --> CACHE_INV
    end

    subgraph Events
        ADDED --> PUB_ADD[Publish ListingsAdd]
        REMOVED --> PUB_REM[Publish ListingsRemove]
    end
```



## Read Path

```mermaid
flowchart TB
    subgraph Query
        REQ[API Request] --> PARSE[Parse world/item IDs]
    end

    subgraph Cache Layer
        PARSE --> MEM_CHECK{In-Memory<br/>Cache Hit?}
        MEM_CHECK -->|Yes| MEM_RETURN[Return Cached]
        MEM_CHECK -->|No| DB_QUERY[Query PostgreSQL]
    end

    subgraph Database
        DB_QUERY --> PG[(PostgreSQL)]
        PG --> RESULTS[Results]
    end

    subgraph Post-Processing
        RESULTS --> MEM_STORE[Store in Cache]
        MEM_STORE --> AGG_UPDATE[Update Aggregates]
        MEM_RETURN --> FORMAT
        AGG_UPDATE --> FORMAT[Format Response]
    end
```

### Query Pattern

```sql
SELECT * FROM listing
WHERE item_id = $1 AND world_id = $2
ORDER BY unit_price ASC
```

For bulk queries:

```sql
SELECT * FROM listing
WHERE item_id = ANY($1) AND world_id = ANY($2)
ORDER BY item_id, world_id, unit_price ASC
```

## Caching Strategy

```mermaid
flowchart LR
    subgraph In-Memory Cache
        MEM[EasyCaching<br/>5-min TTL<br/>100K item limit]
    end

    subgraph Database
        PG[(PostgreSQL)]
    end

    subgraph Aggregates
        REDIS[(Redis<br/>Min Prices)]
    end

    REQUEST --> MEM
    MEM -->|Miss| PG
    PG -->|Store| MEM
    MEM -->|Update| REDIS
```

### Cache Configuration

- **TTL**: 5 minutes
- **Size Limit**: 100,000 items
- **Deep Clone**: Disabled for performance
- **Lock Timeout**: 5 seconds

### Cache Invalidation

- On upload: Existing cache entries are replaced
- Fire-and-forget: Non-blocking updates

## Data Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Active: Upload
    Active --> Active: Re-upload (refresh)
    Active --> Removed: Not in next upload
    Removed --> [*]: Deleted
```

Listings follow a **full replacement** model:

- Each upload for a world/item replaces ALL listings
- Missing listings are considered sold/expired
- No historical listing data is kept

## Metrics

| Metric                                   | Description   |
| ---------------------------------------- | ------------- |
| `universalis_listing_local_cache_hit`    | Cache hits    |
| `universalis_listing_local_cache_miss`   | Cache misses  |
| `universalis_listing_local_cache_update` | Cache updates |
