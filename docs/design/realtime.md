# Real-time Streaming

Universalis provides real-time market updates through WebSocket connections. The system uses a three-layer architecture to efficiently broadcast events to thousands of concurrent clients.

## Three-Layer Architecture

```mermaid
flowchart TB
    subgraph Layer 1: Gateway
        WS_HANDLER[WebSocketHandler<br/>Accept connections]
    end

    subgraph Layer 2: Broker
        PROCESSOR[SocketProcessor<br/>Manage connections<br/>Serialize once<br/>Broadcast to all]
    end

    subgraph Layer 3: Per-Connection
        CLIENT1[SocketClient 1<br/>Filter & queue]
        CLIENT2[SocketClient 2<br/>Filter & queue]
        CLIENT3[SocketClient N<br/>Filter & queue]
    end

    WS_HANDLER --> PROCESSOR
    PROCESSOR --> CLIENT1
    PROCESSOR --> CLIENT2
    PROCESSOR --> CLIENT3
```

### Layer 1: WebSocketHandler

- Accepts WebSocket upgrade requests
- Creates TaskCompletionSource for connection lifecycle
- Delegates to SocketProcessor for management

### Layer 2: SocketProcessor

- **Connection Registry**: ConcurrentDictionary of all active clients
- **Single Serialization**: Each message serialized once, cached bytes reused
- **Parallel Broadcast**: Sends to all clients in parallel
- **Metrics**: Tracks connection count, messages sent, queue latency

### Layer 3: SocketClient

- **Per-Connection State**: Subscription filters, message queue
- **Dual Async Loops**: Outbound (send) and Inbound (receive commands)
- **Priority Queue**: Bounded FIFO for pending messages
- **Backpressure**: Drops oldest messages when queue full

## Event Flow

```mermaid
sequenceDiagram
    participant Upload
    participant RabbitMQ
    participant Dispatcher
    participant Processor
    participant Client
    participant Browser

    Upload->>RabbitMQ: Publish event
    RabbitMQ->>Dispatcher: Consume
    Dispatcher->>Processor: Publish(message)
    Note over Processor: Serialize once<br/>Cache bytes
    Processor->>Client: Enqueue (parallel)
    Note over Client: Filter by subscription
    Client->>Browser: Send if matched
```

## MassTransit Integration

Events flow through RabbitMQ using MassTransit:

```mermaid
flowchart LR
    subgraph Publishers
        UPLOAD[Upload Pipeline]
    end

    subgraph Message Bus
        RABBIT[RabbitMQ]
    end

    subgraph Dispatchers
        ITEM[ItemUpdateDispatcher]
        LIST_ADD[ListingsAddDispatcher]
        LIST_REM[ListingsRemoveDispatcher]
        SALES[SalesAddDispatcher]
    end

    subgraph Broadcast
        SOCKET[SocketProcessor]
    end

    UPLOAD --> RABBIT
    RABBIT --> ITEM
    RABBIT --> LIST_ADD
    RABBIT --> LIST_REM
    RABBIT --> SALES
    ITEM --> SOCKET
    LIST_ADD --> SOCKET
    LIST_REM --> SOCKET
    SALES --> SOCKET
```

## Event Subscription

Clients subscribe to events using a flexible filter syntax:

```mermaid
stateDiagram-v2
    [*] --> Connected
    Connected --> Subscribed: subscribe message
    Subscribed --> Filtered: receive event
    Filtered --> Matched: filter passes
    Filtered --> Dropped: filter fails
    Matched --> Sent: enqueue & send
    Subscribed --> Unsubscribed: unsubscribe message
    Unsubscribed --> Subscribed: subscribe message
```

### Subscription Format

```
channel{property=value,property=value}
```

Examples:

- `listings/add{world=74,item=5}` - Listings for specific world/item
- `sales/add` - All new sales
- `item/update` - All item updates

### Filter Matching

- Hierarchical channels: `listings` matches `listings/add`
- Property filters applied via reflection
- Automatic subscription upgrade/downgrade

## Message Types

| Channel           | Event Type     | Payload                      |
| ----------------- | -------------- | ---------------------------- |
| `item/update`     | ItemUpdate     | World, item, listings, sales |
| `listings/add`    | ListingsAdd    | New listings with details    |
| `listings/remove` | ListingsRemove | Removed listing IDs          |
| `sales/add`       | SalesAdd       | New sales with details       |

## Performance Optimizations

### Cached Serialization

Messages are serialized to BSON once and the bytes are cached:

- Avoids re-serializing for each client
- Memory pooling via RecyclableMemoryStreamManager

### Parallel Broadcast

- Uses `Parallel.ForEach` with `ProcessorCount * 2` parallelism
- Each client receives reference to same cached bytes
- 

### Priority Queue Backpressure

- Queue limited to 30 messages per client
- Oldest messages dropped when full (not newest)
- Prevents slow clients from consuming memory
- Metrics track discarded message count

## Metrics

| Metric                              | Description                          |
| ----------------------------------- | ------------------------------------ |
| `universalis_ws_connections`        | Active connection count              |
| `universalis_ws_sent`               | Total messages sent                  |
| `universalis_ws_exceptions`         | Connection errors                    |
| `universalis_ws_queue_milliseconds` | Broadcast latency                    |
| `universalis_ws_discarded_messages` | Messages dropped due to backpressure |
