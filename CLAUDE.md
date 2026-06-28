# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains learning materials for the Azure DP-420 (Azure Cosmos DB developer) certification exam. It includes C# console applications demonstrating various Azure Cosmos DB SDK operations, connection patterns, and optimization techniques, backed by a Docker-based Cosmos DB emulator environment with multiple pre-seeded databases.

## Architecture

### Projects Structure

- **`src/Dp420_23_IndexOptimization/`** — Console app demonstrating index optimization and batch upsert operations with products and tags. Uses Gateway connection mode and measures RU consumption.
- **`src/DP420_26_Sdk_Troubleshoot/`** — Console app demonstrating SDK connection patterns, bulk execution, error handling (409 Conflict, 403 Forbidden, rate limiting), and CRUD operations with interactive menu.

### Database Environment

- **Docker Compose stack**: Cosmos DB emulator, Azurite (blob/queue storage), Redis, optional observability (OpenTelemetry, Prometheus, Grafana)
- **Data initialization**: Cosmos DB emulator auto-runs `.csh` scripts from `cosmos-init/` on first start (alphabetical order)
- **Four database versions** for different schema demonstrations:
  - **database-v1**: Normalized schema (9 containers, ~210k documents) — customer, addresses, passwords, products, categories, tags, sales orders
  - **database-v2**: Hybrid schema (5 containers, ~51k documents) — flatter structure
  - **database-v3**: Similar to v2 — testing variations
  - **database-v4**: Denormalized schema (3 containers, ~51k documents) — customer includes embedded salesOrders

### Technology Stack

- **.NET 10.0** — Target framework for all projects
- **Microsoft.Azure.Cosmos 3.61.0** — Cosmos DB SDK
- **Configuration**: User secrets + appsettings.json for Key/Endpoint
- **Central package management**: `Directory.Packages.props` (`.NET 8.0+` feature, version pinning for all projects)
- **Python 3**: Data loader for seeding large containers with embedded arrays

## Development Workflow

### Start the Emulator

Run all development services:

```bash
docker compose -f docker-compose.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

Run with observability tools (Grafana, Prometheus, OpenTelemetry):

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

Stop services:

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 down
```

Access Cosmos DB Explorer at `https://localhost:1234` (port from `.env.dev`).

### Configure Projects

Each project requires `Key` and `Endpoint` for Cosmos DB connection. Set via user secrets:

```bash
# For IndexOptimization project
cd src/Dp420_23_IndexOptimization
dotnet user-secrets set "Key" "<account-key>"
dotnet user-secrets set "Endpoint" "<endpoint-url>"

# For SDK Troubleshoot project
cd src/DP420_26_Sdk_Troubleshoot
dotnet user-secrets set "Key" "<account-key>"
dotnet user-secrets set "Endpoint" "<endpoint-url>"
```

For local emulator:
- **Endpoint**: `https://localhost:8081/`
- **Key**: `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyZLgiCeO7v51JRopQ==`

### Build and Run

```bash
# Build all projects
dotnet build

# Run a specific project
cd src/Dp420_23_IndexOptimization
dotnet run

# Or with configuration override
dotnet run -- --Key "<key>" --Endpoint "<endpoint>"
```

### Seed Data

Two methods depending on container size:

**Shell scripts** (small/flat documents):
- Auto-runs from `cosmos-init/` on emulator first start
- Each database version is in its own subfolder: `cosmos-init/database-v1/`, etc.
- **Copy contents of target database folder into `cosmos-init/` root before starting emulator** (remove after emulator starts to avoid re-seeding)

**Python loader** (large documents, embedded arrays):

```bash
# Install SDK once
python3 -m pip install azure-cosmos

# Run loader (emulator must be running first)
python3 load-data.py \
  --endpoint https://localhost:8081/ \
  --key "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyZLgiCeO7v51JRopQ==" \
  --database database-v4 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v4
```

The loader is idempotent (safe to re-run) and retries on timeout.

### Connect to Cosmos Shell

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 exec cosmos cosmoshell.sh
```

## Key Concepts for DP-420 Studies

### Connection Patterns

- **Gateway mode** — simpler setup, single hop, used in `Dp420_23_IndexOptimization`
- **Direct mode** — lower latency, requires TCP connectivity
- **Connection strings** — `AccountEndpoint=...;AccountKey=...` format used in `DP420_26_Sdk_Troubleshoot`

### Configuration & Resilience

- `AllowBulkExecution` — batch operations for throughput efficiency
- `MaxRetryAttemptsOnRateLimitedRequests` — 429 handling (default 3, typically set to 50 in examples)
- `MaxRetryWaitTimeOnRateLimitedRequests` — backoff window for rate limiting

### Error Handling

Common exceptions to handle:

| Status | Class | Reason | Example |
|--------|-------|--------|---------|
| 409 | `Conflict` | Duplicate partition key / item already exists | Re-insert same ID |
| 403 | `Forbidden` | Request forbidden — firewall, storage quota, non-data operations | Check network/quota |
| 429 | `TooManyRequests` | Rate limited — RU exhausted | Retry with backoff |
| 503 | `ServiceUnavailable` | Transient service issue | Retry |
| 408 | `RequestTimeout` | Timeout before server response | Retry |
| 404 | `NotFound` | Document doesn't exist | Handle gracefully |

### Request Units (RU)

- Returned in `ItemResponse<T>.RequestCharge`
- Measured in `Dp420_23_IndexOptimization` example
- Varies by operation size, partition key selectivity, indexing policy

### Partition Key Strategy

- Always specify in point reads (`GetItemAsync`, `DeleteItemAsync`)
- v4 schema denormalizes order history into customer document — reduces cross-partition reads
- Affects query efficiency and multi-item transaction limits

## File Organization

```
src/
├── Dp420_23_IndexOptimization/
│   ├── Product.cs               # Model for products
│   ├── Tag.cs                   # Model for tags
│   ├── Program.cs               # Upsert product example + RU measurement
│   ├── sample.json              # Sample product document
│   ├── appsettings.json         # Config template
│   └── Dp420_23_IndexOptimization.csproj
└── DP420_26_Sdk_Troubleshoot/
    ├── CustomerInfo.cs          # Model for customers
    ├── Program.cs               # CRUD menu + error handling examples
    ├── appsettings.json         # Config template
    └── DP420_26_Sdk_Troubleshoot.csproj

cosmos-init/
├── 01-init-database-v1.csh      # Create database-v1
├── 11-init-database-v2.csh      # Create database-v2
├── 17-init-database-v3.csh      # Create database-v3
├── 23-init-database-v4.csh      # Create database-v4
└── database-v{1-4}/             # Data files for each schema version

mslearn-cosmosdb-modules-central-main/
└── data/fullset/
    └── database-v{1-4}/         # JSON data to seed

load-data.py                      # Python loader for large datasets
```

## Configuration & Secrets

### `.env.dev` — Docker Compose environment

Key variables:
- `VOLUMES_PATH` — Mount point for persistent data (Cosmos, Azurite)
- `HTTPS_CERT_PATH`, `HTTPS_CERT_NAME_*` — Dev SSL certificates
- `COSMOS_CONTAINER_PORT` — Emulator API port (8081)
- `COSMOS_EXPLORER_PORT` — Web UI port (1234)
- `COSMOS_ENABLE_OTLP` — Enable OpenTelemetry export

### User Secrets per Project

Store in `~/.microsoft/usersecrets/{UserSecretsId}/`:

```json
{
  "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyZLgiCeO7v51JRopQ==",
  "Endpoint": "https://localhost:8081/"
}
```

Retrieve via `IConfigurationRoot` in Program.cs.

## Observability

Optional Docker Compose extension adds:

- **Prometheus** — metrics collection
- **Grafana** — visualization (`http://localhost:4000`)
- **OpenTelemetry Collector** — trace/metric aggregation
- **Aspire Dashboard** — .NET diagnostic dashboard

Enable in `.env.dev`: `COSMOS_ENABLE_OTLP=true`

## Common Tasks

**Run a project against the emulator:**

```bash
# Start emulator
docker compose -f docker-compose.yaml --env-file .env.dev -p cosmos-dp420 up --build

# In another terminal, run project
cd src/Dp420_23_IndexOptimization && dotnet run
```

**Inspect a container via Cosmos Explorer:**

1. Navigate to `https://localhost:1234`
2. Browse databases and containers in the left sidebar
3. Query or view documents

**Seed database-v4 from JSON:**

```bash
python3 load-data.py \
  --endpoint https://localhost:8081/ \
  --key "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyZLgiCeO7v51JRopQ==" \
  --database database-v4 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v4
```

**Reset Cosmos DB emulator data:**

```bash
# Stop services
docker compose down

# Remove data volume
rm -rf ~/Works/var/cosmosdp420/cosmos/data

# Restart
docker compose up
```

## Testing Against Azure (Production)

When migrating from emulator to real Cosmos DB:

1. Update secrets with production endpoint and key
2. Remove development-specific options: `ConnectionMode = Gateway`, `IgnoreSslCertificateValidation`
3. Adjust throughput and indexing policy for production workload
4. Test rate-limit retry logic (real account has stricter RU limits)
5. Verify partition key distribution on larger datasets

## Skills & MCP Resources

Custom skills available for Cosmos DB guidance:
- `/cosmosdb-best-practices` — Performance, partitioning, query optimization
- `/cosmosdb-sdk` — SDK client setup and patterns
- `/cosmosdb-data-and-queries` — Query design and data modeling
- `/cosmosdb-operations` — Throughput, monitoring, global distribution

Use these when designing schemas, optimizing queries, or troubleshooting performance.

## MCP in this repository

- Claude repository MCP setup is defined in `.claude/settings.json` under `mcpServers`.
- `context7` is configured to run via:

```json
"context7": {
  "command": "npx",
  "args": ["-y", "@upstash/context7-mcp"]
}
```

- Copilot workspace MCP config is shared in `.mcp.json` so Copilot sessions started from this repository can discover the same Context7 server.
- Keep `.claude/settings.local.json` for developer-specific local overrides only (it is intentionally not committed).
