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

The repository `.mcp.json` is configured for **Streamable HTTP** (`type: "http"`, `url: "http://127.0.0.1:6128/"`) — so start the shell command above before using MCP tools.

### Environment file (`.env.dev`)

Every `docker compose` command below passes `--env-file .env.dev` — the stack won't start without it. It's gitignored and never committed, so create it yourself in the repo root before the first run. These are the variables `docker-compose.yaml` and `docker-compose.observability.yaml` actually read:

```bash
# Host path where Cosmos/Azurite data persists across restarts
VOLUMES_PATH=~/path/to/some/writable/dir

# Ports exposed on the host
COSMOS_CONTAINER_HEALTH_PORT=8080
COSMOS_CONTAINER_PORT=8081
COSMOS_EXPLORER_PORT=1234

# Azurite (blob/queue/table emulator) connection string — well-known default, not a secret
AZURITE_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1

# Certificate filenames from the "Self-signed certificate" section below — must exist under ./certs
HTTPS_CERT_NAME_CRT=dev1.crt
HTTPS_CERT_NAME_KEY=dev1.key
HTTPS_CERT_NAME_PEM=dev1.pem

# Observability stack only (docker-compose.observability.yaml)
COSMOS_ENABLE_OTLP=true
ASPIRE_DASHBOARD_OTLP_PRIMARYAPIKEY=myprimaryapikey
```

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

### Self-signed certificate

Before start need to generate self-signed certificates.
More detail in an
official [docs](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-openssl)

```bash
#!/bin/bash

PARENT="dev1"

# Array of DNS entries
DNS_ENTRIES=(
    "localhost"
    "aspire-dashboard"
    "seq"
    "jaeger"
    "cadvisor"
    "prometheus"
    "aspire-dashboard"
    "otel-collector"
)

# Generate the DNS entries with proper numbering
DNS_SECTION=""
ORDER=1
for DNS in "${DNS_ENTRIES[@]}"; do
    DNS_SECTION+="DNS.${ORDER} = ${DNS}\n"
    ((ORDER++))
    DNS_SECTION+="DNS.${ORDER} = www.${DNS}\n"
    ((ORDER++))
done

openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 365 \
-nodes \
-keyout $PARENT.key \
-out $PARENT.crt \
-subj "/CN=${PARENT}" \
-extensions v3_ca \
-extensions v3_req \
-config <( \
  echo '[req]'; \
  echo 'default_bits= 4096'; \
  echo 'distinguished_name=req'; \
  echo 'x509_extension = v3_ca'; \
  echo 'req_extensions = v3_req'; \
  echo '[v3_req]'; \
  echo 'basicConstraints = CA:FALSE'; \
  echo 'keyUsage = nonRepudiation, digitalSignature, keyEncipherment'; \
  echo 'subjectAltName = @alt_names'; \
  echo '[ alt_names ]'; \
  echo -e "${DNS_SECTION}"; \
  echo '[v3_ca]'; \
  echo 'subjectKeyIdentifier=hash'; \
  echo 'authorityKeyIdentifier=keyid:always,issuer'; \
  echo 'basicConstraints = critical, CA:TRUE, pathlen:0'; \
  echo 'keyUsage = critical, cRLSign, keyCertSign'; \
  echo 'extendedKeyUsage = serverAuth, clientAuth')

openssl x509 -noout -text -in $PARENT.crt
openssl x509 -in $PARENT.crt -out $PARENT.pem -outform PEM

openssl pkcs12 -export -out dev1.pfx -inkey dev1.key -in dev1.crt
```

Generate certificates

```bash
mkdir certs && cd certs
chmod +x certs.sh
./certs.sh
```

Install certs in your system

- on Mac OS

```bash
rm -rf ~/.aspnet
dotnet dev-certs https --trust --verbose

sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/dev1.crt
sudo security import certs/dev1.key -k /Library/Keychains/System.keychain
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
