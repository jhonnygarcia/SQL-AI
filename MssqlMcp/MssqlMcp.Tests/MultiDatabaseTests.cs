// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public sealed class MultiDatabaseTests : IDisposable
    {
        private const string Primary = "primary";
        private const string Secondary = "secondary";

        private readonly string _tableName;
        private readonly Tools _tools;

        public MultiDatabaseTests()
        {
            _tableName = $"TestTable_{Guid.NewGuid():N}";

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{Primary}"] = Environment.GetEnvironmentVariable("CONNECTION_STRING"),
                    [$"ConnectionStrings:{Secondary}"] = Environment.GetEnvironmentVariable("CONNECTION_STRING_2"),
                })
                .Build();

            var connectionFactory = new SqlConnectionFactory(configuration);
            var loggerMock = new Mock<ILogger<Tools>>();
            _tools = new Tools(connectionFactory, loggerMock.Object);
        }

        public void Dispose()
        {
            // The table only ever exists in the secondary database, so clean up there.
            var _ = _tools.DropTable($"DROP TABLE IF EXISTS {_tableName}", Secondary).GetAwaiter().GetResult();
        }

        [Fact]
        public void ListDatabases_ReturnsBothNames()
        {
            var result = _tools.ListDatabases();
            Assert.True(result.Success);

            var names = Assert.IsAssignableFrom<IReadOnlyList<string>>(result.Data);
            Assert.Contains(Primary, names);
            Assert.Contains(Secondary, names);
        }

        [Fact]
        public async Task CreateTable_TargetsTheNamedDatabaseOnly()
        {
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)", Secondary);
            Assert.True(createResult.Success);

            var secondaryTables = await _tools.ListTables(Secondary);
            Assert.True(secondaryTables.Success);
            var secondaryNames = Assert.IsAssignableFrom<List<string>>(secondaryTables.Data);
            Assert.Contains(secondaryNames, name => name.EndsWith($".{_tableName}", StringComparison.OrdinalIgnoreCase));

            var primaryTables = await _tools.ListTables(Primary);
            Assert.True(primaryTables.Success);
            var primaryNames = Assert.IsAssignableFrom<List<string>>(primaryTables.Data);
            Assert.DoesNotContain(primaryNames, name => name.EndsWith($".{_tableName}", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ReadData_ReturnsError_WhenDatabaseIsOmittedAndSeveralAreConfigured()
        {
            var result = await _tools.ReadData("SELECT 1");
            Assert.False(result.Success);
            Assert.Contains("'database' argument is required", result.Error ?? string.Empty, StringComparison.Ordinal);
            Assert.Contains(Primary, result.Error ?? string.Empty, StringComparison.Ordinal);
            Assert.Contains(Secondary, result.Error ?? string.Empty, StringComparison.Ordinal);
        }
    }
}
