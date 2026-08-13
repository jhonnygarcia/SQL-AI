# MSSQL MCP Server (.NET 8)

A [Model Context Protocol](https://github.com/modelcontextprotocol/csharp-sdk) server that lets an
AI agent work with SQL Server and Azure SQL databases over stdio.

## What it does

Eight tools, over one or more databases you configure by name:

| Tool | |
| --- | --- |
| `ListDatabases` | names of the configured databases |
| `ListTables` | tables in a database |
| `DescribeTable` | columns, types and keys of a table |
| `ReadData` | run a query |
| `InsertData` / `UpdateData` | write rows |
| `CreateTable` / `DropTable` | change the schema |

Every tool except `ListDatabases` takes a `database` argument. With one database configured you can
omit it; with two or more it is required, and a call without it returns the list of names.

## Install

### 1. Download

Grab your platform's binary from
[Releases](https://github.com/jhonnygarcia/SQL-AI/releases) — it is self-contained, so **no .NET
runtime is needed**.

| Platform | Asset | |
| --- | --- | --- |
| Windows | `MssqlMcp-win-x64.exe` | SmartScreen warns on first run: "More info" > "Run anyway" |
| Linux | `MssqlMcp-linux-x64` | `chmod +x` it |
| macOS (Apple Silicon) | `MssqlMcp-osx-arm64` | `chmod +x` it, then `xattr -d com.apple.quarantine` |

Put it somewhere stable — you reference its **absolute path** in the config below.

### 2. Configure your client

Databases come from environment variables named `ConnectionStrings__<name>` (**double** underscore).
The name is what you say to the agent: "list the tables in *sales*".

**Claude Code** — the CLI writes the config for you (add `-s user` for all projects):

```sh
claude mcp add mssql \
  --env ConnectionStrings__sales="Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True" \
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

Restart the client and run `/mcp` in Claude Code — `mssql` should show as connected with 8 tools.
Then ask: *"using mssql, list the tables in sales"*.

## Troubleshooting

**The server fails to start.** Run the binary by hand; it logs to stderr and waits for input:

```sh
ConnectionStrings__sales="Server=.;Database=Sales;..." ./MssqlMcp
```

If you see "Application started" (Ctrl+C to stop), the binary is fine and the problem is the config:
wrong absolute path, or single-quoted backslashes in JSON. On macOS an unquarantined binary is
killed silently — see the download table.

**Tools appear but every call errors.** The message comes straight from SQL Server: a login failure,
an unreachable host, or a certificate complaint (`TrustServerCertificate=True` for a local server
with a self-signed certificate).

**A database seems missing.** Check the double underscore in `ConnectionStrings__sales` — a single
one is ignored silently.

**Azure SQL sign-in loops or prompts repeatedly.** Prefer `Authentication=Active Directory Default`
over `Active Directory Interactive`, which prompts once per distinct connection string. If "Default"
fails with "Task canceled", fall back to "Interactive".

## Development

```sh
cd MssqlMcp
dotnet build
dotnet test          # needs CONNECTION_STRING and CONNECTION_STRING_2 pointing at two real databases
```

Instead of environment variables you can copy `MssqlMcp/appsettings.example.json` to
`appsettings.json` (gitignored — it holds credentials) next to the executable:

```json
{
  "ConnectionStrings": {
    "sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
    "hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Environment variables override that file, and a bare `CONNECTION_STRING` still works — it registers
a database named `default`.

## Releasing

Push a `v*` tag; `.github/workflows/release.yml` cross-publishes the three binaries and creates the
GitHub release:

```sh
git tag v1.0.1 && git push origin v1.0.1
```

The tag name becomes the assembly version. Each binary is ~76 MB because it bundles the runtime;
trimming stays off since `Microsoft.Data.SqlClient` breaks under it.
