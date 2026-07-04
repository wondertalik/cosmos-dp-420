# Introduction

Cosmos implementation.

## DEV WORKFLOW

### Run during development

We use [docker compose](https://docs.docker.com/compose/) to run dependencies.

From a root directory of project run commands:

- install CosmosDBShell version used by both Copilot and Claude MCP clients

The published NuGet versions (through at least `1.1.115-preview`) have bugs that break Claude Code (invalid MCP protocol version, and a `CancellationTokenSource` disposal bug that fails every MCP tool call). **For now**, build and install the locally-patched fork instead of a plain `dotnet tool install`/`update` — see `ai-context/SPEC.md`'s "Known issue #1" and "Known issue #2" for the exact commands. Confirm with:

```bash
dotnet tool install --global --add-source "cosmosdbshell" CosmosDBShell --version "1.1.123-preview.g8da79a2066"
```

```bash
cosmosdbshell --version
# -> CosmosDBShell 1.1.123-preview (8da79a2066)  [or later, once the upstream PRs ship]
```

- run Cosmos DB Shell MCP in a separate terminal (required for local Cosmos vNext emulator MCP access)

```bash
cosmosdbshell --mcp --connect "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" --connect-mode gateway
```

The repository `.mcp.json` is configured for **Streamable HTTP** (`type: "http"`, `url: "http://127.0.0.1:6128/"`), not SSE — so start the shell command above before using MCP tools.

- run all development services

```bash
docker compose -f docker-compose.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

- run all development services with observability tools

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 up --build --remove-orphans
```

- stop and remove all services

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 down
```

- connect to cosmos db shell

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p cosmos-dp420 exec cosmos cosmoshell.sh
```

## SEEDING DATA

### How it works

The emulator auto-runs `.csh` scripts from `cosmos-init/` on first start (alphabetical order). The init scripts create the databases and containers. Data is loaded either via the shell scripts or the Python loader (see below).

Because `cosmos-init/` must be flat (no subdirectories), each database has its own subfolder (`cosmos-init/database-v1/`, etc.). **Copy the contents of the target database folder into `cosmos-init/` before starting the emulator**, then remove them after.

The emulator skips init on subsequent starts if the data volume (`${VOLUMES_PATH}/cosmos/data`) already contains data.

### Shell scripts (small containers only)

Shell scripts work for containers with small/flat documents. Containers with embedded arrays (e.g. `salesOrder` with embedded `details`) time out on the emulator for large datasets — use the Python loader for those.

### Python loader (large or embedded-array containers)

Install once:

```bash
python3 -m pip install azure-cosmos
```

The loader connects directly to the running emulator, upserts each document with retry on timeout, and is safe to re-run (skips already-inserted docs).

**The emulator must be running before you execute the loader.**

Both `--endpoint` and `--key` are required. Run one database at a time:

- database-v1

```bash
python3 load-data.py \
  --endpoint <endpoint> --key <account-key> \
  --database database-v1 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v1
```

- database-v2

```bash
python3 load-data.py \
  --endpoint <endpoint> --key <account-key> \
  --database database-v2 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v2
```

- database-v3

```bash
python3 load-data.py \
  --endpoint <endpoint> --key <account-key> \
  --database database-v3 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v3
```

- database-v4

```bash
python3 load-data.py \
  --endpoint <endpoint> --key <account-key> \
  --database database-v4 \
  --data-dir ./mslearn-cosmosdb-modules-central-main/data/fullset/database-v4
```

### Data volumes per database

| Database | Containers seeded | Total documents |
|---|---|---|
| database-v1 | customer, customerAddress, customerPassword, product, productCategory, productTag, productTags, salesOrder, salesOrderDetail | 210,827 |
| database-v2 | customer, product, productCategory, productTag, salesOrder | 51,116 |
| database-v3 | customer, product, productCategory, productTag, salesOrder | 51,116 |
| database-v4 | customer (includes salesOrders), product, productMeta | 51,116 |
