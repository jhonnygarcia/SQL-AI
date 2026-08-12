
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

Download the binary for your platform from the [Releases page](../../releases) and put it anywhere
you like — it is self-contained, so **no .NET runtime is required**.

| Platform | File |
| --- | --- |
| Windows | `MssqlMcp.exe` |
| Linux | `MssqlMcp` (`chmod +x MssqlMcp`) |
| macOS (Apple Silicon) | `MssqlMcp` (`chmod +x MssqlMcp`) |

On Windows the first run shows a SmartScreen warning because the binary is unsigned — choose
"More info" > "Run anyway". On macOS, run `xattr -d com.apple.quarantine MssqlMcp` once.

Then point your MCP client at that path and give it a connection string, as shown below. Every
client takes the same two things: the path to the executable, and `ConnectionStrings__<name>`
environment variables — one per database you want to expose.

### Claude Code

Add the server with the CLI:

```sh
claude mcp add mssql --env ConnectionStrings__sales="Server=.;Database=Sales;Trusted_Connection=True;TrustServerCertificate=True" -- C:\tools\MssqlMcp.exe
```

Or commit a `.mcp.json` at the root of a repo so everyone on the team gets it:

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

Check it loaded with `/mcp` inside Claude Code, then ask "using mssql, list the tables in sales".

### opencode

Add it to `opencode.json` in the project, or to `~/.config/opencode/opencode.json` to have it
everywhere:

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

1. If you get a "Task canceled" error using "Active Directory Default", try "Active Directory Interactive".
2. With several Azure SQL databases configured, prefer `Authentication=Active Directory Default` over
   `Active Directory Interactive`. Interactive authentication prompts once per distinct connection
   string, so five databases means five sign-in prompts.
3. If `appsettings.json` seems to be ignored, check that it was copied next to `MssqlMcp.exe` — the
   server reads it from the executable's directory, not from the working directory.



