// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "List Databases",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists the names of the configured databases. Any of these names can be passed as the 'database' argument of the other tools.")]
    public DbOperationResult ListDatabases()
        // Names only. Connection strings carry credentials and must never reach the client.
        => new(success: true, data: _connectionFactory.Databases);
}
