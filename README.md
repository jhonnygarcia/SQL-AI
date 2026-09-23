# MSSQL MCP Server (.NET 8)

A [Model Context Protocol](https://modelcontextprotocol.io) server that lets an AI agent list,
describe, query and modify tables in SQL Server and Azure SQL. It speaks MCP over stdio and can
work with **several databases at once**, each configured under a name you choose.

Built on the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk). The source
lives in [`MssqlMcp/`](MssqlMcp).

## Tools

| Tool | | Writes |
| --- | --- | --- |
| `ListDatabases` | names of the configured databases | |
| `ListTables` | tables in a database | |
| `DescribeTable` | columns, types and keys of a table | |
| `ReadData` | run a `SELECT` query | |
| `InsertData` / `UpdateData` | write rows | ✔ |
| `CreateTable` / `DropTable` | change the schema | ✔ |

Every tool except `ListDatabases` takes a `database` argument. With one database configured you can
omit it; with two or more it is required, and a call without it returns the list of names.

`ReadData` only runs plain `SELECT` queries (CTEs, `UNION` and `FOR JSON` included). Anything that
could change state is rejected before it reaches the server: DML, DDL, `EXEC`, `SELECT ... INTO`,
`OPENQUERY` / `OPENROWSET` / `OPENDATASOURCE` and `NEXT VALUE FOR`. Set [`ReadOnly`](#read-only-mode)
to hide the write tools altogether.

## Install

### 1. Download

Grab your platform's binary from [Releases](https://github.com/jhonnygarcia/SQL-AI/releases). It is
self-contained, so **no .NET runtime is needed**.

| Platform | Asset | |
| --- | --- | --- |
| Windows | `MssqlMcp-win-x64.exe` | SmartScreen warns on first run: "More info" > "Run anyway" |
| Linux | `MssqlMcp-linux-x64` | `chmod +x` it |
| macOS (Apple Silicon) | `MssqlMcp-osx-arm64` | `chmod +x` it, then `xattr -d com.apple.quarantine` |

Put it somewhere stable — you reference its **absolute path** in the config below. (Prefer to build
it yourself? See [Build from source](#build-from-source).)

### 2. Configure your client

Databases come from environment variables named `ConnectionStrings__<name>` (**double** underscore),
one per database. The name is what you say to the agent: "list the tables in *sales*".

**Claude Code** — the CLI writes the config for you (add `-s user` for all projects):

```sh
claude mcp add mssql \
  --env ConnectionStrings__sales="Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True" \
  --env ConnectionStrings__hr="Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True" \
  -- C:\tools\MssqlMcp.exe
```

**Claude Desktop** (File > Settings > Developer > Edit Config) and shared `.mcp.json` files use the
same shape:

```json
{
  "mcpServers": {
    "mssql": {
      "command": "C:\\tools\\MssqlMcp.exe",
      "env": {
        "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
        "ConnectionStrings__hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
      }
    }
  }
}
```

**opencode** — edit `opencode.json` in the project, or `~/.config/opencode/opencode.json`
(`%USERPROFILE%\.config\opencode\opencode.json` on Windows) for all of them. The shape differs:
`mcp` instead of `mcpServers`, `command` is an array, variables go under `environment`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "mssql": {
      "type": "local",
      "command": ["C:\\tools\\MssqlMcp.exe"],
      "enabled": true,
      "environment": {
        "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True"
      }
    }
  }
}
```

**VS Code** — Ctrl+Shift+P > "Preferences: Open Settings (JSON)", then add the server under
`mcp.servers` with `"type": "stdio"` and the same `command` / `env` pair as Claude Desktop.

Backslashes must be doubled inside JSON. Don't commit a password in a shared file — use Windows auth
or Entra ID.

Connection string examples:

| Scenario | |
| --- | --- |
| Local SQL Server, Windows auth | `Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True` |
| SQL login | `Server=myhost,1433;Database=Sales;User Id=sa;Password=***;TrustServerCertificate=True` |
| Azure SQL, Entra ID | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=Sales;Encrypt=Mandatory;Authentication=Active Directory Default` |

### 3. Verify

Restart the client and run `/mcp` in Claude Code — `mssql` should show as connected with 8 tools
(4 in [read-only mode](#read-only-mode)).
Then ask: *"using mssql, list the tables in sales"*.

## Configuration reference

Settings follow the standard .NET cascade; later sources override earlier ones:

1. `appsettings.json` next to the executable
2. environment variables (`ConnectionStrings__<name>`)
3. command-line arguments (`--ConnectionStrings:<name>=...`)

A bare `CONNECTION_STRING` environment variable is also honored and registers a database named
`default`.

To use a file instead of environment variables, copy
[`MssqlMcp/MssqlMcp/appsettings.example.json`](MssqlMcp/MssqlMcp/appsettings.example.json) to
`appsettings.json` next to the executable (it is gitignored — it holds credentials):

```json
{
  "ConnectionStrings": {
    "sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
    "hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "ReadOnly": false
}
```

### Read-only mode

Set `ReadOnly` to `true` and the server never registers `InsertData`, `UpdateData`, `CreateTable` or
`DropTable` — the client sees only `ListDatabases`, `ListTables`, `DescribeTable` and `ReadData`. It
follows the same cascade as everything else: `"ReadOnly": true` in `appsettings.json`, a `ReadOnly`
environment variable, or `--ReadOnly=true`:

```sh
claude mcp add mssql-prod \
  --env ReadOnly=true \
  --env ConnectionStrings__prod="Server=tcp:myserver.database.windows.net,1433;Initial Catalog=Prod;Encrypt=Mandatory;Authentication=Active Directory Default" \
  -- C:\tools\MssqlMcp.exe
```

The switch covers the whole server. To keep some databases writable, register a second server entry
without it. The server-side checks are a guardrail. For a hard guarantee, also connect with a login
that can only read (for example, a user in `db_datareader` only).

## Troubleshooting

**The server fails to start.** Run the binary by hand; it logs to stderr and waits for input:

```sh
ConnectionStrings__sales="Server=.;Database=Sales;..." ./MssqlMcp
```

If you see "Application started" (Ctrl+C to stop), the binary is fine and the problem is the config:
wrong absolute path, or single-quoted backslashes in JSON. On macOS a quarantined binary is killed
silently — see the download table.

**Tools appear but every call errors.** The message comes straight from SQL Server: a login failure,
an unreachable host, or a certificate complaint (`TrustServerCertificate=True` for a local server
with a self-signed certificate).

**A database seems missing.** Check the double underscore in `ConnectionStrings__sales` — a single
one is ignored silently.

**Azure SQL sign-in loops or prompts repeatedly.** Prefer `Authentication=Active Directory Default`
over `Active Directory Interactive`, which prompts once per distinct connection string. If "Default"
fails with "Task canceled", fall back to "Interactive".

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
cd MssqlMcp
dotnet build
dotnet publish MssqlMcp -c Release -r win-x64 -o out   # or linux-x64 / osx-arm64
```

Point your client's `command` at `out/MssqlMcp.exe` (or `out/MssqlMcp`).

### Tests

Tests hit **real databases** — there is no fake. They create and drop GUID-suffixed `TestTable_*`
tables, and need two databases so the multi-database tests can prove a call only touches the one it
names:

```sh
sqlcmd -S . -Q "IF DB_ID('test') IS NULL CREATE DATABASE test; IF DB_ID('test2') IS NULL CREATE DATABASE test2"
```

```sh
export CONNECTION_STRING="Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
export CONNECTION_STRING_2="Server=.;Database=test2;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test                                          # all tests
dotnet test --filter "FullyQualifiedName~ReadData"   # a single test or class
```

(On Windows `cmd`, use `SET CONNECTION_STRING=...` without quotes.)

## Releasing

Push a `v*` tag; [`.github/workflows/release.yml`](.github/workflows/release.yml) cross-publishes the
three binaries and creates the GitHub release:

```sh
git tag v1.1.0 && git push origin v1.1.0
```

The tag name becomes the assembly version. Each binary is ~76 MB because it bundles the runtime;
trimming stays off since `Microsoft.Data.SqlClient` breaks under it.

## License

[MIT](LICENSE). Originally derived from Microsoft's
[Azure-Samples/SQL-AI-samples](https://github.com/Azure-Samples/SQL-AI-samples).
