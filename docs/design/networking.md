# Networking

This document covers the network architecture, ingress path, and routing patterns for Universalis.

## Network Topology

```mermaid
flowchart TB
    subgraph Internet
        USERS[Users]
        CF[Cloudflare<br/>TLS Termination]
    end

    subgraph Edge
        LB[Load Balancer]
    end

    subgraph Private Network
        subgraph Managers
            T1[Traefik]
            T2[Traefik]
            T3[Traefik]
        end

        subgraph Services
            API[API Services]
            WS[WebSocket Services]
        end

        subgraph Databases
            DB[(Databases)]
        end
    end

    USERS --> CF
    CF --> LB
    LB --> T1
    LB --> T2
    LB --> T3
    T1 --> API
    T2 --> WS
    T3 --> API
    API --> DB
```

### Network Isolation

| Zone     | Purpose      | Access                  |
| -------- | ------------ | ----------------------- |
| Public   | User traffic | Internet via Cloudflare |
| Private  | Service mesh | Internal only           |
| Database | Data stores  | Services only           |

## Ingress Path

```mermaid
sequenceDiagram
    participant User
    participant Cloudflare
    participant LB as Load Balancer
    participant Traefik
    participant Service

    User->>Cloudflare: HTTPS Request
    Note over Cloudflare: TLS Termination<br/>DDoS Protection
    Cloudflare->>LB: HTTP (internal)
    LB->>Traefik: Round Robin
    Note over Traefik: Routing Rules<br/>Path Matching
    Traefik->>Service: Forward Request
    Service-->>User: Response
```

### TLS Termination

TLS is terminated at Cloudflare:

- Simplifies certificate management
- Enables DDoS protection
- Internal traffic uses HTTP

## Load Balancing

### External Load Balancer

```mermaid
flowchart LR
    LB[Load Balancer]
    M1[Manager 1]
    M2[Manager 2]
    M3[Manager 3]

    LB -->|Round Robin| M1
    LB --> M2
    LB --> M3
```

- Round-robin distribution to manager nodes
- Health checks remove unhealthy managers
- Single public IP for all traffic

### Traefik Routing

Traefik runs on all manager nodes and handles:

- Path-based routing to services
- Host-based routing for subdomains
- WebSocket upgrades
- Metrics endpoint

## Routing Patterns

### Path-Based Routing

```mermaid
flowchart LR
    subgraph Incoming
        REQ[Request]
    end

    subgraph Traefik Rules
        API_RULE["/api/*"]
        WS_RULE["/ws"]
        DOCS_RULE["/docs/*"]
    end

    subgraph Services
        API_SVC[API Service]
        WS_SVC[WebSocket Service]
        DOCS_SVC[Docs Service]
    end

    REQ --> API_RULE
    REQ --> WS_RULE
    REQ --> DOCS_RULE
    API_RULE --> API_SVC
    WS_RULE --> WS_SVC
    DOCS_RULE --> DOCS_SVC
```

### Host-Based Routing

| Host                 | Service       |
| -------------------- | ------------- |
| universalis.app      | Main API      |
| docs.universalis.app | Documentation |

### Service Discovery

```mermaid
flowchart TB
    TRAEFIK[Traefik] --> SWARM[Docker Swarm API]
    SWARM --> DNS[Service DNS]
    DNS --> TASKS[tasks.service-name]
    TASKS --> REPLICAS[Service Replicas]
```

Docker Swarm provides:

- Automatic service registration
- DNS-based discovery (`tasks.<service>`)
- Health-aware routing

## Internal Communication

### Service to Service

```mermaid
flowchart LR
    subgraph Services
        API[API Service]
        WS[WebSocket Service]
    end

    subgraph Message Bus
        RABBIT[RabbitMQ]
    end

    subgraph Databases
        PG[(PostgreSQL)]
        REDIS[(Redis)]
    end

    API -->|Private Network| PG
    API -->|Private Network| REDIS
    API -->|MassTransit| RABBIT
    RABBIT --> WS
```

- All internal traffic uses private network
- No public exposure of databases
- Message bus for async communication

### Database Connectivity

| Database   | Access Pattern                        |
| ---------- | ------------------------------------- |
| PostgreSQL | Direct connection, connection pooling |
| ScyllaDB   | Cluster-aware driver, multiple nodes  |
| Redis      | Multiplexed connections               |
| RabbitMQ   | AMQP over internal network            |

## Firewall Rules

```mermaid
flowchart TB
    subgraph Allowed Inbound
        HTTP[HTTP/HTTPS<br/>From Load Balancer]
        SWARM[Swarm Ports<br/>Between Nodes]
    end

    subgraph Blocked
        SSH_EXT[SSH from Internet]
        DB_EXT[Database from Internet]
    end

    HTTP --> ALLOW[Allow]
    SWARM --> ALLOW
    SSH_EXT --> DENY[Deny]
    DB_EXT --> DENY
```

Key rules:

- HTTP/HTTPS from load balancer only
- Swarm communication between nodes
- SSH restricted to management IPs
- Databases not publicly accessible

## WebSocket Handling

```mermaid
sequenceDiagram
    participant Client
    participant Traefik
    participant WSService

    Client->>Traefik: HTTP Upgrade: websocket
    Traefik->>WSService: Forward Upgrade
    WSService-->>Client: 101 Switching Protocols
    Note over Client,WSService: Persistent Connection
    Client->>WSService: Subscribe messages
    WSService->>Client: Event stream
```

Traefik handles WebSocket upgrades transparently:

- Detects upgrade headers
- Maintains persistent connections
- Forwards bidirectional traffic
