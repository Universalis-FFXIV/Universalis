# Infrastructure

Universalis runs on a Docker Swarm cluster with workload-specific service variants. This document covers the cluster architecture and deployment patterns.

## Cluster Architecture

```mermaid
flowchart TB
    subgraph Manager Nodes
        M1[Manager 1]
        M2[Manager 2]
        M3[Manager 3]
    end

    subgraph Worker Nodes
        W1[Worker Pool]
    end

    subgraph Database Nodes
        DB1[Database Pool]
    end

    M1 <--> M2
    M2 <--> M3
    M3 <--> M1

    M1 --> W1
    M2 --> W1
    M3 --> W1

    W1 --> DB1
```

### Node Types

| Type | Role |
|------|------|
| Manager | Cluster orchestration, Traefik ingress, scheduling |
| Worker | Application services |
| Database | Dedicated database workloads (ScyllaDB, PostgreSQL) |

## Service Variant Pattern

The same application codebase runs as multiple services, each optimized for a specific workload.

```mermaid
flowchart TB
    subgraph API Services
        MAIN[Main API<br/>General purpose]
        WS[WebSocket Service<br/>Event streaming]
        HISTORY[History Service<br/>Time-series queries]
        UPLOAD[Upload Service<br/>Data ingestion]
    end

    subgraph Testing Services
        CANARY[Canary<br/>Safe deployment testing]
        STAGING[Staging<br/>Pre-production]
    end

    subgraph Shared Resources
        PG[(PostgreSQL)]
        SCYLLA[(ScyllaDB)]
        REDIS[(Redis)]
        RABBIT[RabbitMQ]
    end

    MAIN --> PG
    MAIN --> REDIS
    WS --> RABBIT
    HISTORY --> SCYLLA
    UPLOAD --> PG
    UPLOAD --> SCYLLA
    UPLOAD --> RABBIT

    CANARY -.-> PG
    STAGING -.-> PG
```

### Service Purposes

| Service | Workload | Optimization |
|---------|----------|--------------|
| Main API | General queries | Balanced resources |
| WebSocket | Event streaming | High connection count, low memory per connection |
| History | Time-series queries | Large memory for result sets, ScyllaDB tuning |
| Upload | Data ingestion | Large buffers, write-optimized |
| Canary | Deployment testing | Single replica for safe rollout |
| Staging | Pre-production | Minimal resources |

### Why Separate Services?

```mermaid
flowchart LR
    subgraph Benefits
        SCALE[Independent Scaling<br/>Scale history reads separately]
        ISOLATE[Resource Isolation<br/>Upload spikes don't affect queries]
        TUNE[Workload Tuning<br/>Different thread pool sizes]
        DEPLOY[Safe Deployments<br/>Canary before production]
    end
```

1. **Independent Scaling**: Scale history service during high query load without scaling uploads
2. **Resource Isolation**: Upload spikes don't starve query resources
3. **Workload Tuning**: Each service has optimized thread pool, buffer sizes, connection limits
4. **Safe Deployments**: Canary service tests changes before full rollout

## Deployment Flow

```mermaid
flowchart LR
    DEV[Development] --> STAGING[Staging]
    STAGING --> CANARY[Canary]
    CANARY --> PRODUCTION[Production Rollout]

    STAGING -->|Issues| DEV
    CANARY -->|Issues| DEV
```

### Canary Pattern

```mermaid
flowchart TB
    LB[Load Balancer]

    subgraph Canary
        CAN[Canary Service<br/>New Version]
    end

    subgraph Production
        PROD1[Production 1]
        PROD2[Production 2]
        PROD3[Production N]
    end

    LB -->|Small %| CAN
    LB -->|Most traffic| PROD1
    LB --> PROD2
    LB --> PROD3
```

- Small percentage of traffic routes to canary
- Monitor for errors before full rollout
- Easy rollback by removing canary routing

## Configuration by Service

Each service variant has different configuration:

### Thread Pool
- Main API: High thread count for concurrent requests
- Upload: Moderate threads, focus on I/O
- WebSocket: Low threads, async I/O

### Buffer Sizes
- Upload: Large read/write buffers for data ingestion
- History: Large result buffers for big queries
- WebSocket: Small buffers, many connections

### Connection Pools
- Main API: Balanced PostgreSQL connections
- History: More ScyllaDB connections
- Upload: Write-focused connection allocation

## Backup Strategy

```mermaid
flowchart LR
    PG[(PostgreSQL)] --> DUMP[pg_dump]
    DUMP --> S3[(S3 Storage)]

    SCYLLA[(ScyllaDB)] --> SNAP[Snapshots]
    SNAP --> S3
```

- PostgreSQL: Daily dumps to S3-compatible storage
- ScyllaDB: Built-in snapshot mechanism
- Redis: Configurable persistence (AOF/RDB)

## Health Checks

Each service exposes health endpoints:
- Readiness: Can accept traffic
- Liveness: Process is healthy

Swarm uses these for:
- Rolling updates without downtime
- Automatic restart of failed containers
- Load balancer routing decisions
