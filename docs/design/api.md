# API Architecture

The Universalis API serves market board data through a versioned REST API with support for real-time updates via WebSocket.

## API Versioning

Three API versions coexist to maintain backward compatibility while evolving the API:

| Version  | Status  | Description                                |
| -------- | ------- | ------------------------------------------ |
| V1       | Legacy  | Original API, maintained for compatibility |
| V2       | Current | Primary API with WebSocket support         |
| V3 (WIP) | Newest  | Refactored response structure              |

```mermaid
graph LR
    subgraph API Versions
        V1["/api/v1/*<br/>Legacy"]
        V2["/api/v2/*<br/>Current"]
        V3["/api/v3/*<br/>Newest"]
    end

    subgraph Shared Services
        DB[(Database Layer)]
        CACHE[(Cache Layer)]
        GAME[Game Data]
    end

    V1 --> DB
    V2 --> DB
    V3 --> DB
    V1 --> CACHE
    V2 --> CACHE
    V3 --> CACHE
    V1 --> GAME
    V2 --> GAME
    V3 --> GAME
```

## Request Lifecycle

```mermaid
sequenceDiagram
    participant Client
    participant Traefik
    participant Middleware
    participant Controller
    participant Service
    participant Database

    Client->>Traefik: HTTP Request
    Traefik->>Middleware: Route to Service
    Middleware->>Middleware: Exception Filters
    Middleware->>Controller: Route to Action
    Controller->>Controller: Parameter Validation
    Controller->>Service: Business Logic
    Service->>Database: Data Access
    Database-->>Service: Results
    Service-->>Controller: Domain Objects
    Controller-->>Client: JSON Response
```

## Controller Organization

Controllers are organized by API version and domain:

```mermaid
graph TB
    subgraph V3 Controllers
        V3_MARKET[Market Controllers]
        V3_GAME[Game Controllers]
    end

    subgraph V2 Controllers
        V2_CURRENT[CurrentlyShown]
        V2_HISTORY[History]
        V2_UPLOAD[Upload]
        V2_WS[WebSocket]
    end

    subgraph V1 Controllers
        V1_COMPAT[Legacy Compatibility]
    end

    subgraph Domains
        LISTINGS[Listings Domain]
        SALES[Sales Domain]
        METADATA[Game Metadata]
    end

    V3_MARKET --> LISTINGS
    V3_MARKET --> SALES
    V3_GAME --> METADATA

    V2_CURRENT --> LISTINGS
    V2_HISTORY --> SALES
    V2_UPLOAD --> LISTINGS
    V2_UPLOAD --> SALES

    V1_COMPAT --> LISTINGS
    V1_COMPAT --> SALES
```

## Key Endpoints

### Market Data

- `GET /api/{world}/{itemIds}` - Current listings and recent sales
- `GET /api/history/{world}/{itemIds}` - Historical sales with filtering
- `GET /api/v2/history/{world}/{itemIds}` - Enhanced history with statistics

### Game Data

- `GET /api/v3/game/data-centers` - List of data centers
- `GET /api/v3/game/worlds` - List of worlds

### Real-time

- `GET /api/ws` - WebSocket connection for live updates
- `POST /upload/{apiKey}` - Upload market data

## Request Processing

### Exception Handling

Five specialized exception filters handle different error scenarios:

- Parameter validation errors
- Authentication failures
- Rate limiting
- Database errors
- Unexpected exceptions

### Response Formatting

- JSON serialization with custom converters
- Partial field selection support
- Compression for large responses
- CORS headers for browser clients

## Query Parameters

Common parameters across endpoints:

| Parameter       | Description                  |
| --------------- | ---------------------------- |
| `listings`      | Number of listings to return |
| `entries`       | Number of history entries    |
| `hq`            | Filter by high-quality items |
| `statsWithin`   | Time window for statistics   |
| `entriesWithin` | Time filter for history      |
