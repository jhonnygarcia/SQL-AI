// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Update Data",
        ReadOnly = false,
        Destructive = true),
        Description("Updates data in a table in the SQL Database. Expects a valid UPDATE SQL statement as input.")]
    public async Task<DbOperationResult> UpdateData(
        [Description("UPDATE SQL statement")] string sql,
        [Description("Name of the configured database to run against. Call ListDatabases to see the available names.")] string? database = null)
    {
        try
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync(database);
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                var rows = await cmd.ExecuteNonQueryAsync();
                return new DbOperationResult(true, null, rows);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateData failed on database {Database}: {Message}", database ?? "(default)", ex.Message);
            return new DbOperationResult(false, ex.Message);
        }
    }
}

