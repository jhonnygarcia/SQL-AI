# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo shape

The repo holds a single project: `MssqlMcp/`, an MCP server for SQL Server / Azure SQL (originally
forked from Microsoft's `SQL-AI-samples`; the other samples were removed). The root `README.md` is
the user-facing documentation (install, client config, troubleshooting, build, release) — keep it in
sync when tools or configuration change. `.github/workflows/release.yml` publishes binaries on `v*`
tags.

## MssqlMcp — MCP server for SQL Server / Azure SQL

.NET 10 console app using the official MCP C# SDK (`ModelContextProtocol` 2.x), speaking MCP over
**stdio**. `MssqlMcp/global.json` pins the SDK to 10.0 and opts `dotnet test` into the Microsoft
Testing Platform runner that xunit.v3 needs (the old VSTest path errors on the .NET 10 SDK).
`Microsoft.Data.SqlClient` 7 moved Entra ID auth into `Microsoft.Data.SqlClient.Extensions.Azure`;
keep that package referenced or `Authentication=Active Directory ...` connection strings break.

```sh
cd MssqlMcp
dotnet build
dotnet test                                        # all tests
dotnet test --filter "FullyQualifiedName~ReadData" # single test / class
```

**Tests hit a real database.** `MssqlMcpTests` constructs a real `SqlConnectionFactory`, so
`CONNECTION_STRING` must be set before `dotnet test` or every test fails on connect. Each test
creates a GUID-suffixed `TestTable_*` and drops it in `Dispose`. Only `ILogger` is mocked.

```sh
SET CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True
SET CONNECTION_STRING_2=Server=.;Database=test2;Trusted_Connection=True;TrustServerCertificate=True
```

`CONNECTION_STRING_2` must point at a **second** database — `MultiDatabaseTests` uses it to prove
that a tool call targets only the database it names. Create it once with
`sqlcmd -S . -Q "IF DB_ID('test2') IS NULL CREATE DATABASE test2"`.

Databases are configured by name under a `ConnectionStrings` section, resolved once in the
`SqlConnectionFactory` constructor via `IConfiguration` — not per call. Configuration follows the
standard .NET cascade: `appsettings.json` next to the executable, overridden by environment
variables (e.g. `ConnectionStrings__sales`), overridden by command-line arguments. A bare
`CONNECTION_STRING` variable is still honored as a fallback and registers under the name
`default`. When exactly one database is configured, a tool's `database` argument may be omitted;
when more than one is configured it is required, and a call that omits it returns an error naming
the configured databases. There is no in-memory/fake DB path.

### Architecture

- `Program.cs` — host setup only. Tools are registered with `.WithTools(...)` from
  `ToolRegistry.GetToolMethods(readOnly)`, which reflects over every `[McpServerTool]` method on
  `Tools`, so a new tool needs no registration. When the `ReadOnly` setting is true, tools whose
  attribute says `ReadOnly = false` are left out entirely. `ToolRegistry.CreateTool` binds every
  call to the `Tools` DI singleton (it has no parameterless constructor) and keeps the method name
  as the tool name — the SDK would otherwise publish `read_data`-style snake_case names. Console logging goes to **stderr**
  (stdout is the MCP channel — never `Console.WriteLine` from tool code).
- `ReadOnlySqlValidator` — parses SQL with ScriptDom and accepts only plain `SELECT` batches
  (no `INTO`, `OPENQUERY`/`OPENROWSET`/`OPENDATASOURCE`, `NEXT VALUE FOR`). `ReadData` calls it
  before opening a connection, in every mode.
- `Tools/Tools.cs` — `[McpServerToolType] public partial class Tools`, registered as a DI
  singleton, holds `ISqlConnectionFactory` + `ILogger` via primary constructor.
- `Tools/*.cs` — one file per tool, each a `public partial class Tools` continuation with a
  single `[McpServerTool]`-attributed method. Adding a tool = adding one such file. Tools are
  `ListDatabases`, `ListTables`, `DescribeTable`, `CreateTable`, `DropTable`, `InsertData`,
  `ReadData`, `UpdateData`. Every tool other than `ListDatabases` takes an optional `database`
  argument naming which configured database to run against.
- `DbOperationResult` — every tool returns this (`Success`/`Error`/`RowsAffected`/`Data`).
  Tools catch all exceptions, log them, and return `new DbOperationResult(success: false,
  error: ex.Message)` rather than throwing across the MCP boundary.

### Tool conventions

Match the existing tools exactly when adding one:

- Attribute carries the semantic hints the client uses: `Title`, `ReadOnly`, `Idempotent`,
  `Destructive`, plus a `[Description]` on the method and on every parameter.
- Get a connection per call via `await _connectionFactory.GetOpenConnectionAsync(database)` inside
  `using (conn)`, passing the tool's `database` argument through; ADO.NET pooling handles reuse —
  do not cache connections. The acquisition call must sit inside the `try` block: name resolution
  throws for an unknown or omitted-but-required database, and a tool must never throw across the
  MCP boundary.
- The `ReadOnly` hint is load-bearing: it decides whether the tool is exposed in read-only mode.
  Set it to `false` on anything that can change state.
- Data-manipulation tools (`ReadData`, `InsertData`, `UpdateData`, `CreateTable`, `DropTable`)
  take a raw SQL string by design; `ReadData`'s is validated as SELECT-only. Metadata tools (`DescribeTable`, `ListTables`) query `sys.*`
  views with `@`-parameters — keep parameterizing there.

### Style

`MssqlMcp/.editorconfig` is authoritative and strict: `Nullable` and `ImplicitUsings` enabled,
4-space indent, System usings first, no `this.`, and an enforced MIT file header on every `.cs`
file:

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
```
