# Upload Pipeline

The upload pipeline processes market data from game clients, validates it, and distributes it to storage and real-time subscribers.

## Upload Flow Overview

```mermaid
flowchart TB
    subgraph Authentication
        REQ[Upload Request] --> AUTH[API Key Validation]
        AUTH --> HASH[SHA-512 Key Hash]
        HASH --> TRUST{Trusted Source?}
        TRUST -->|No| REJECT[Reject]
        TRUST -->|Yes| FLAG{Flagged Uploader?}
        FLAG -->|Yes| SILENT[Silent Ignore]
        FLAG -->|No| PROCESS[Process Upload]
    end

    subgraph Processing
        PROCESS --> ANON[Anonymize Uploader ID<br/>SHA-256]
        ANON --> VALIDATE[Validator Behaviors]
        VALIDATE --> ASYNC[Async Behaviors]
    end

    VALIDATE --> EVENTS[Publish Events]
```

## Behavior Pattern Architecture

Uploads are processed through a pipeline of behaviors implementing `IUploadBehavior`. Behaviors are classified as either **validators** (blocking) or **async** (fire-and-forget).

```mermaid
flowchart LR
    subgraph Validator Behaviors
        direction TB
        V1[WorldIdUploadBehavior<br/>Validate world exists]
        V2[ItemIdUploadBehavior<br/>Validate item is marketable]
        V3[MarketBoardUploadBehavior<br/>Main processing]
    end

    subgraph Async Behaviors
        direction TB
        A1[DailyUploadIncrement<br/>Daily stats]
        A2[SourceIncrement<br/>Per-source metrics]
        A3[MostRecentlyUpdated<br/>Track freshness]
        A4[PlayerContent<br/>Character tracking]
        A5[TaxRates<br/>City tax tracking]
    end

    INPUT[Upload Data] --> V1
    V1 --> V2
    V2 --> V3
    V3 --> PARALLEL{Parallel Execution}
    PARALLEL --> A1
    PARALLEL --> A2
    PARALLEL --> A3
    PARALLEL --> A4
    PARALLEL --> A5
```

## Behavior Execution

```mermaid
sequenceDiagram
    participant Controller
    participant Validators
    participant MarketBoard
    participant AsyncBehaviors
    participant MessageBus

    Controller->>Validators: Execute in sequence
    Note over Validators: Can reject request
    Validators->>MarketBoard: Process data
    MarketBoard->>MarketBoard: Clean & dedupe
    MarketBoard->>MessageBus: Publish events
    MarketBoard-->>Controller: Complete
    Controller->>AsyncBehaviors: Task.WhenAll (fire-and-forget)
    Note over AsyncBehaviors: Non-blocking
```

## Data Cleaning

The MarketBoardUploadBehavior performs extensive cleaning:

### Listing Cleaning

- Generate listing IDs if missing (SHA-256 hash of key fields)
- Remove HTML tags from names
- Validate materia slots and IDs
- Normalize unusual boolean/integer formats

### Sale Cleaning

- Validate timestamps (reject future dates)
- Validate prices and quantities
- Remove duplicates against existing history

## Deduplication

```mermaid
flowchart TB
    subgraph Listing Deduplication
        L_NEW[New Listings] --> L_EXISTING[Fetch Existing]
        L_EXISTING --> L_DIFF[Compute Delta]
        L_DIFF --> L_ADD[Added Listings]
        L_DIFF --> L_REM[Removed Listings]
    end

    subgraph Sale Deduplication
        S_NEW[New Sales] --> S_EXISTING[Fetch History<br/>from ScyllaDB]
        S_EXISTING --> S_FINGERPRINT[SHA-256 Fingerprint]
        S_FINGERPRINT --> S_UNIQUE[Unique Sales Only]
    end

    L_ADD --> EVENTS[Publish Events]
    L_REM --> EVENTS
    S_UNIQUE --> EVENTS
```

## Event Publishing

After processing, events are published to the message bus:

| Event            | Trigger                    | Consumers             |
| ---------------- | -------------------------- | --------------------- |
| `ListingsAdd`    | New listings detected      | WebSocket subscribers |
| `ListingsRemove` | Listings no longer present | WebSocket subscribers |
| `SalesAdd`       | New sales recorded         | WebSocket subscribers |
| `ItemUpdate`     | Any change to item data    | WebSocket subscribers |

### Where the listings diff comes from

The add/remove diff must be taken against the board as it stood *before* the
upload. That state is exactly what the write destroys, so it cannot be fetched
by a separate read — any such read is unordered against the write.

It used to be read back while the write was in flight:

```mermaid
sequenceDiagram
    autonumber
    participant H as HandleListings
    participant P as PublishListingsToMessageBus<br/>(never awaited)
    participant DB as Postgres
    participant C as listings cache
    participant S as Subscriber

    H-)P: start, without awaiting
    P->>DB: SELECT the prior board
    H->>DB: ReplaceLive: DELETE + INSERT

    Note over P,DB: issued first, but on a different pooled<br/>connection - nothing orders the two

    alt SELECT resolves after the write commits
        DB-->>P: the rows that were just written
        Note over P: old == new, so<br/>added = [] and removed = []
        P--xS: no frame at all - the change is lost silently
    else SELECT resolves before the write commits
        DB-->>P: the pre-upload rows
        P->>S: listings/remove + listings/add (correct)
        DB->>C: evict listing5:{world}:{item}
        P->>C: store the pre-upload rows, after the evict
        Note over C: REST serves a stale board<br/>for the 5-minute TTL
    end
```

Neither branch is safe: one loses the frame, the other poisons the cache. The
write now returns the rows it displaced instead, at no extra round-trip:

```mermaid
sequenceDiagram
    autonumber
    participant H as HandleListings
    participant P as PublishListingsToMessageBus<br/>(never awaited)
    participant DB as Postgres
    participant C as listings cache
    participant S as Subscriber

    H->>DB: ReplaceLive: DELETE ... RETURNING + INSERT<br/>(one batch, one implicit transaction)
    DB-->>H: the rows the DELETE displaced =<br/>the board as it was before this upload
    DB->>C: evict listing5:{world}:{item}

    Note over H: added = uploaded - displaced<br/>removed = displaced - uploaded

    H-)P: publish, after the write has landed
    P->>S: listings/remove + listings/add

    Note over H,DB: no read, so nothing to race
    Note over DB,C: retained retainer rows were never deleted,<br/>so they cannot be reported as removals
```

`RETURNING` on a `DELETE` yields the rows as they last existed, which is the
one statement where "what `RETURNING` gives" and "the state before the write"
coincide. On an `INSERT` or `UPDATE` it yields the *new* row instead — so
rewriting this path as an upsert would silently invert the diff.

# 
