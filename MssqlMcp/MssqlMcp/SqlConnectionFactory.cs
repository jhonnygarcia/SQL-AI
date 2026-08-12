// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Mssql.McpServer;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private const string ConnectionStringsSection = "ConnectionStrings";
    private const string LegacyConnectionStringKey = "CONNECTION_STRING";
    private const string LegacyDatabaseName = "default";

    private readonly Dictionary<string, string> _connectionStrings;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in configuration.GetSection(ConnectionStringsSection).GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                _connectionStrings[entry.Key] = entry.Value;
            }
        }

        // Backwards compatibility: a bare CONNECTION_STRING becomes the "default" database.
        var legacyConnectionString = configuration[LegacyConnectionStringKey];
        if (!string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            _connectionStrings[LegacyDatabaseName] = legacyConnectionString;
        }

        if (_connectionStrings.Count == 0)
        {
            throw new InvalidOperationException(
                "No databases are configured.\n\nHINT: add a \"ConnectionStrings\" section to appsettings.json, "
                + "or set the CONNECTION_STRING environment variable: "
                + "`SET CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True`");
        }
    }

    public IReadOnlyList<string> Databases => _connectionStrings.Keys.ToList();

    public async Task<SqlConnection> GetOpenConnectionAsync(string? database = null)
    {
        var connectionString = ResolveConnectionString(database);

        // Let ADO.Net handle connection pooling
        var conn = new SqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }

        return conn;
    }

    private string ResolveConnectionString(string? database)
    {
        if (string.IsNullOrWhiteSpace(database))
        {
            return _connectionStrings.Count == 1
                ? _connectionStrings.Values.First()
                : throw new InvalidOperationException(
                    $"The 'database' argument is required. Configured databases: {ConfiguredNames}");
        }

        return _connectionStrings.TryGetValue(database, out var connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                $"Unknown database '{database}'. Configured databases: {ConfiguredNames}");
    }

    private string ConfiguredNames => string.Join(", ", _connectionStrings.Keys);
}
