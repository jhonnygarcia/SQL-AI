
# Mssql SQL MCP Server (.NET 8)

This project is a .NET 8 console application implementing a Model Context Protocol (MCP) server for MSSQL Databases using the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Features

- Configure one or more databases, each under a name, in `appsettings.json` or through environment variables.
- **MCP Tools Implemented**:
  - ListDatabases: List the names of the configured databases.
  - ListTables: List all tables in the database.
  - DescribeTable: Get schema/details for a table.
  - CreateTable: Create new tables.
  - DropTable: Drop existing tables.
  - InsertData: Insert data into tables.
  - ReadData: Read/query data from tables.
  - UpdateData: Update values in tables.

  Every tool other than `ListDatabases` takes an optional `database` argument naming which configured database to run against.
- **Logging**: Console logging using Microsoft.Extensions.Logging.
- **Unit Tests**: xUnit-based unit tests for all major components.

## Install

You need three things: the binary, a connection string, and one config entry in your MCP client.
No .NET runtime, no cloning this repo, no build step — the binary ships everything it needs.

### Step 1 — Download the binary

Grab the file for your platform from the [Releases page](../../releases):

| Platform | Asset | After downloading |
| --- | --- | --- |
| Windows | `MssqlMcp-win-x64.exe` | rename to `MssqlMcp.exe` if you like |
| Linux | `MssqlMcp-linux-x64` | `chmod +x MssqlMcp-linux-x64` |
| macOS (Apple Silicon) | `MssqlMcp-osx-arm64` | `chmod +x MssqlMcp-osx-arm64` |

Put it in a stable location — you will reference this **absolute path** in the config, so a folder
that does not move: `C:\tools\MssqlMcp.exe` on Windows, `~/.local/bin/MssqlMcp` on Linux/macOS.

The binary is unsigned, so the OS warns you the first time:

- **Windows**: SmartScreen shows "Windows protected your PC" > "More info" > "Run anyway".
- **macOS**: run `xattr -d com.apple.quarantine ~/.local/bin/MssqlMcp` once, otherwise it is killed
  on launch and the client just reports that the server failed to start.

### Step 2 — Write your connection string

Each database you expose gets a name and a connection string. The name is what you say to the
agent ("list the tables in **sales**"); the connection string is standard ADO.NET:

| Scenario | Connection string |
| --- | --- |
| Local SQL Server, Windows auth | `Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True` |
| SQL Server, SQL login | `Server=myhost,1433;Database=Sales;User Id=sa;Password=***;TrustServerCertificate=True` |
| Azure SQL, Entra ID | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=Sales;Encrypt=Mandatory;Authentication=Active Directory Default` |

The server reads them from environment variables named `ConnectionStrings__<name>` — note the
**double underscore**. `ConnectionStrings__sales` registers a database called `sales`.

With exactly one database configured you can omit the `database` argument in every tool call. With
two or more it becomes required, and a call that leaves it out comes back with the list of names.

### Step 3a — Configure Claude Code

The fastest path is the CLI, which writes the config for you:

```sh
claude mcp add mssql \
  --env ConnectionStrings__sales="Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True" \
  -- C:\tools\MssqlMcp.exe
```

That registers it for the current project only. Add `-s user` to make it available in every project
on your machine.

To share it with a team instead, commit a `.mcp.json` at the root of the repo — everyone who opens
it gets prompted to enable the server:

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

Backslashes in Windows paths must be doubled inside JSON. Do not commit a `.mcp.json` containing a
password — use Windows auth, Entra ID, or `${ENV_VAR}` expansion for shared files.

### Step 3b — Configure opencode

opencode has no add command; you edit JSON directly. For one project, create `opencode.json` next
to the code. To have it everywhere, edit the global config instead:

- Linux/macOS: `~/.config/opencode/opencode.json`
- Windows: `%USERPROFILE%\.config\opencode\opencode.json`

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "mssql": {
      "type": "local",
      "command": ["C:\\tools\\MssqlMcp.exe"],
      "enabled": true,
      "environment": {
        "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
        "ConnectionStrings__hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
      }
    }
  }
}
```

Note the shape differs from Claude Code: the section is `mcp` (not `mcpServers`), `command` is an
**array** whose first element is the executable, and the variables go under `environment` (not
`env`).

### Step 4 — Verify

Restart the client, then:

- **Claude Code**: run `/mcp`. `mssql` should be listed as connected with 8 tools.
- **opencode**: the tools appear in the session; `opencode` logs a startup error for the server if
  the path or connection string is wrong.

Then ask the agent: *"using mssql, list the tables in sales"*. If it answers with your tables, you
are done. If the server does not start, see [Troubleshooting](#troubleshooting) — the most common
causes are a wrong absolute path, an unquarantined macOS binary, and a single underscore instead of
a double one in `ConnectionStrings__sales`.

## Getting Started

### Prerequisites

- Access to a SQL Server or Azure SQL Database

### Setup

1. **Build ***

---
```sh
   cd MssqlMcp
   dotnet build
```
---

### Configure your databases

Copy `MssqlMcp/appsettings.example.json` to `MssqlMcp/appsettings.json` and list one entry per database.
The file is gitignored because connection strings carry credentials.

---
```json
{
  "ConnectionStrings": {
    "sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
    "hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```
---

Then ask the agent "using MSSQL MCP, list the tables in sales".

When a single database is configured, the `database` argument may be omitted. When more than one is
configured it is required, and a call without it comes back with the list of configured names.

2. VSCode: **Start VSCode, and add MCP Server config to VSCode Settings**

Load the settings file in VSCode (Ctrl+Shift+P > Preferences: Open Settings (JSON)).

Add a new MCP Server with the following settings:

---
```json
    "MSSQL MCP": {
        "type": "stdio",
        "command": "C:\\src\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
        "env": {
            "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
            "ConnectionStrings__hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
            }
}
```
---

NOTE: Replace the path "C:\\src\\SQL-AI-samples" with the location of your SQL-AI-samples repo on your machine.

e.g. your MCP settings should look like this if "MSSQL MCP" is your own MCP Server in VSCode settings:

---
```json
"mcp": {
    "servers": {
        "MSSQL MCP": {
            "type": "stdio",
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
                "env": {
                "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
                "ConnectionStrings__hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
            }
    }
}
```
---

An example of using a connection string for Azure SQL Database:
---
```json
"mcp": {
    "servers": {
        "MSSQL MCP": {
            "type": "stdio",
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
                "env": {
                "ConnectionStrings__sales": "Server=tcp:<servername>.database.windows.net,1433;Initial Catalog=<databasename>;Encrypt=Mandatory;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Interactive"
            }
    }
}
```
---

Environment variables override `appsettings.json`, and the bare `CONNECTION_STRING` variable still works — it registers as a database named `default`.

**Run the MCP Server**

Save the Settings file, and then you should see the "Start" button appear in the settings.json.  Click "Start" to start the MCP Server. (You can then click on "Running" to view the Output window).

Start Chat (Ctrl+Shift+I), make sure Agent Mode is selected.

Click the tools icon, and ensure the "MSSQL MCP" tools are selected.

Then type in the chat window "List tables in the database" and hit enter. (If you have other tools loaded, you may need to specify "MSSQL MCP" in the initial prompt, e.g. "Using MSSQL MCP, list tables").

3. Claude Desktop: **Add MCP Server config to Claude Desktop**

Press File > Settings > Developer.
Press the "Edit Config" button (which will load the claude_desktop_config.json file in your editor).

Add a new MCP Server with the following settings:

---
```json
{
    "mcpServers": {
        "MSSQL MCP": {
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
            "env": {
                    "ConnectionStrings__sales": "Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True",
                    "ConnectionStrings__hr": "Server=.;Database=HR;Trusted_Connection=True;TrustServerCertificate=True"
                }
        }
    }
}
```
---

Save the file, start a new Chat, you'll see the "Tools" icon, it should list 8 MSSQL MCP tools.

# Cutting a release

Push a `v*` tag and `.github/workflows/release.yml` builds the three binaries and creates the GitHub
release for you:

```sh
git tag v1.0.0 && git push origin v1.0.0
```

The manual equivalent, if you'd rather do it by hand: publishing produces one self-contained single-file executable per platform (~76 MB each — that is
the price of not requiring a .NET runtime on the user's machine). Trimming is deliberately off:
`Microsoft.Data.SqlClient` breaks when trimmed.

```sh
cd MssqlMcp/MssqlMcp
dotnet publish -c Release -r win-x64   -o out/win-x64
dotnet publish -c Release -r linux-x64 -o out/linux-x64
dotnet publish -c Release -r osx-arm64 -o out/osx-arm64

# the three binaries share a name, so label them as they are uploaded
gh release create v1.0.0 \
  "out/win-x64/MssqlMcp.exe#MssqlMcp-win-x64.exe" \
  "out/linux-x64/MssqlMcp#MssqlMcp-linux-x64" \
  "out/osx-arm64/MssqlMcp#MssqlMcp-osx-arm64" \
  --title "MSSQL MCP v1.0.0" \
  --notes "Download the binary for your platform and follow the Install section of the README."
```

Bump `AssemblyVersion` / `FileVersion` / `InformationalVersion` in `MssqlMcp.csproj` to match the tag
before publishing.

# Troubleshooting

**The client says the server failed to start.** Run the binary by hand from a terminal — it prints
its startup log to stderr and waits for input, which tells you far more than the client does:

```sh
ConnectionStrings__sales="Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True" ./MssqlMcp
```

You should see "Application started". Press Ctrl+C to stop it. If that works but the client still
fails, the problem is the config: check the absolute path, and that Windows backslashes are doubled
in JSON.

**The tools are listed but every call errors.** The connection string is reaching the server but the
database is refusing it. The error text comes straight from SQL Server — a login failure, an
unreachable host, or a certificate complaint (add `TrustServerCertificate=True` for a local server
with a self-signed certificate).

**`ListDatabases` returns nothing, or calls complain about a missing `database` argument.** The
environment variable name is wrong. It is `ConnectionStrings__sales` with a **double** underscore;
a single one is silently ignored.

**macOS kills the binary on launch.** It is quarantined because it is unsigned:
`xattr -d com.apple.quarantine MssqlMcp`.

1. If you get a "Task canceled" error using "Active Directory Default", try "Active Directory Interactive".
2. With several Azure SQL databases configured, prefer `Authentication=Active Directory Default` over
   `Active Directory Interactive`. Interactive authentication prompts once per distinct connection
   string, so five databases means five sign-in prompts.
3. If `appsettings.json` seems to be ignored, check that it was copied next to `MssqlMcp.exe` — the
   server reads it from the executable's directory, not from the working directory.



