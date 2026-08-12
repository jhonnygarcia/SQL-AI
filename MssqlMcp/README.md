
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

# Troubleshooting

1. If you get a "Task canceled" error using "Active Directory Default", try "Active Directory Interactive".
2. With several Azure SQL databases configured, prefer `Authentication=Active Directory Default` over
   `Active Directory Interactive`. Interactive authentication prompts once per distinct connection
   string, so five databases means five sign-in prompts.
3. If `appsettings.json` seems to be ignored, check that it was copied next to `MssqlMcp.exe` — the
   server reads it from the executable's directory, not from the working directory.



