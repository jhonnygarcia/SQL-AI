// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Insert Data",
        ReadOnly = false,
        Destructive = false),
        Description("Updates data in a table in the SQL Database. Expects a valid INSERT SQL statement as input. ")]
    public async Task<DbOperationResult> InsertData(
        [Description("INSERT SQL statement")] string sql,
        [Description("Name of the configured database to run against. Call ListDatabases to see the available names.")] string? database = null)
    {
        try
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync(database);
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                var rows = await cmd.ExecuteNonQueryAsync();
                return new DbOperationResult(success: true, rowsAffected: rows);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertData failed on database {Database}: {Message}", database ?? "(default)", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
