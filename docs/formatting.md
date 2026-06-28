# C# Formatting

This repository uses:

- root `.editorconfig` for baseline formatting and code-style rules
- `scripts/format-csharp.sh` for ReSharper CLI-based cleanup/formatting

## Prerequisites

Install ReSharper Command Line Tools:

```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools --version 2026.1.0
```

Verify:

```bash
jb --version
```

## Formatter script

Run from repository root:

```bash
./scripts/format-csharp.sh
```

### Common examples

Format all projects:

```bash
./scripts/format-csharp.sh
```

Format specific projects:

```bash
./scripts/format-csharp.sh -p "DP420_13_ChangeFeed,DP420_26_Sdk_Troubleshoot"
```

Format specific files:

```bash
./scripts/format-csharp.sh -f "src/DP420_13_ChangeFeed/Program.cs,src/DP420_26_Sdk_Troubleshoot/Program.cs"
```

Format with explicit solution path:

```bash
./scripts/format-csharp.sh -s "CosmosDP420.slnx"
```

Use another cleanup profile:

```bash
./scripts/format-csharp.sh --profile "Built-in: Full Cleanup"
```

Show help:

```bash
./scripts/format-csharp.sh -h
```

## Notes

- The script supports both `.slnx` and `.sln`.
- If no solution file is found, it falls back to `src/**/*.csproj`.
- By default, generated files and `bin/`/`obj/` content are excluded.

## Troubleshooting

If `jb` is not found:

```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools --version 2026.1.0
```

If project selection does not match, try:

1. exact project name from `.csproj` file name
2. partial name that appears in the project path
3. omitting `-p` to format all projects
