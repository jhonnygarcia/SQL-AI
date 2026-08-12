// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;
public partial class Tools
{
    [McpServerTool(
        Title = "Read Data",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data")]
    public async Task<DbOperationResult> ReadData(
        [Description("SQL query to execute")] string sql,
        [Description("Name of the configured database to run against. Call ListDatabases to see the available names.")] string? database = null)
    {
        try
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync(database);
            using (conn)
            {
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
                return new DbOperationResult(success: true, data: results);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadData failed on database {Database}: {Message}", database ?? "(default)", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
