# Copilot Instructions for ConsoleDP420

## Build, test, and lint commands

```bash
# Build all projects in the solution
dotnet build

# Build one project
dotnet build src/Dp420_23_IndexOptimization/Dp420_23_IndexOptimization.csproj
dotnet build src/DP420_26_Sdk_Troubleshoot/DP420_26_Sdk_Troubleshoot.csproj

# Run a project
dotnet run --project src/Dp420_23_IndexOptimization
dotnet run --project src/DP420_26_Sdk_Troubleshoot
```

There are currently no test projects and no dedicated lint command configured in this repository.

If a test project is added, use:

```bash
# Full test project
dotnet test path/to/Project.Tests.csproj

# Single test
dotnet test path/to/Project.Tests.csproj --filter "FullyQualifiedName~TestName"
```

## MCP setup in this repository

- Workspace MCP config is committed at `.mcp.json`.
- Claude MCP config is committed at `.claude/settings.json`.
- `context7` is configured as a local stdio MCP server:

```json
"context7": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@upstash/context7-mcp"],
  "tools": ["*"]
}
```

- Useful CLI checks:

```bash
# List all MCP servers and their source (user/workspace/plugin/builtin)
gh copilot -- mcp list

# JSON output for automation/debugging
gh copilot -- mcp list --json
```

- `.mcp.json` also configures a `cosmosdb` MCP server (Streamable HTTP, `http://127.0.0.1:6128/`) backed by `cosmosdbshell --mcp` running in its own terminal. That server keeps its connection alive across tool calls for the whole session, so **do not call `connect` at the start of a session just because it seems safe** — it's redundant for this setup and can be counterproductive. Only call `connect` if a command actually fails due to no active connection.

## High-level architecture

- This repo is a DP-420 learning workspace centered on **Azure Cosmos DB emulator workflows**, with two independent .NET console apps in `src/`:
  - `Dp420_23_IndexOptimization`: upsert flow for product documents and RU charge visibility.
  - `DP420_26_Sdk_Troubleshoot`: interactive CRUD + Cosmos error-handling scenarios (409/403/429/503/408/404).
- Local infra is Docker Compose based:
  - `docker-compose.yaml`: Cosmos emulator + Azurite + Redis.
  - `docker-compose.observability.yaml`: optional observability stack (Prometheus, Jaeger, Seq, OTEL collector, Aspire dashboard).
- Data setup uses two paths:
  - `cosmos-init/*.csh`: init scripts auto-run on emulator first startup.
  - `load-data.py`: external bulk upsert loader for larger/embedded JSON datasets from `mslearn-cosmosdb-modules-central-main/data/fullset`.
- Dependency management is centralized via `Directory.Packages.props` (projects reference packages without per-project versions).

## Key repository conventions

- **Configuration source pattern**: both apps build `IConfigurationRoot` from `appsettings.json` + `AddUserSecrets<Program>()`, and require `Key`/`Endpoint` values.
- **Console app style**: `Program.cs` files use top-level statements and local async helper methods instead of class-based program structure.
- **Cosmos SDK usage is explicit and demo-oriented**:
  - Container/database names are hardcoded for learning scenarios.
  - Partition keys are explicitly passed in point operations (`CreateItemAsync`, `DeleteItemAsync`).
  - `DP420_26_Sdk_Troubleshoot` intentionally enables `AllowBulkExecution` and high retry settings to demonstrate throttling/transient handling.
- **Modeling style differs by scenario**:
  - `Dp420_23_IndexOptimization` uses immutable `record` models (`Product`, `Tag`) deserialized from `sample.json`.
  - `DP420_26_Sdk_Troubleshoot` uses mutable POCO with `Newtonsoft.Json` property mapping (`CustomerInfo`).
- **Sample-data flow convention** (`Dp420_23_IndexOptimization`): `sample.json` contains `<unique-identifier>` placeholder replaced at runtime before deserialization/upsert.
- **Framework baseline**: all .NET projects target `net10.0`.
