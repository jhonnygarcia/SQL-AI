// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Defines a factory interface for creating SQL database connections.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Opens a connection to one of the configured databases.
    /// </summary>
    /// <param name="database">
    /// Name of the configured database. May be omitted only when a single database is configured.
    /// </param>
    Task<SqlConnection> GetOpenConnectionAsync(string? database = null);

    /// <summary>
    /// Gets the names of the configured databases.
    /// </summary>
    IReadOnlyList<string> Databases { get; }
}
