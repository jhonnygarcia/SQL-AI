// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Configuration;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public class SqlConnectionFactoryTests
    {
        // Never actually reached except by the case-insensitivity test, which only needs it to fail fast.
        private const string FakeConnectionString = "Server=fake;Database=fake;Trusted_Connection=True;Connect Timeout=1";

        private static SqlConnectionFactory CreateFactory(params string[] names)
        {
            var values = new Dictionary<string, string?>();
            foreach (var name in names)
            {
                values[$"ConnectionStrings:{name}"] = FakeConnectionString;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new SqlConnectionFactory(configuration);
        }

        [Fact]
        public void Databases_ReturnsEveryConfiguredName()
        {
            var factory = CreateFactory("sales", "hr");

            // Sort before asserting: Databases' enumeration order is not a documented contract.
            Assert.Equal(new[] { "hr", "sales" }, factory.Databases.OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void Constructor_Throws_WhenNothingIsConfigured()
        {
            var configuration = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<InvalidOperationException>(() => new SqlConnectionFactory(configuration));
            Assert.Contains("No databases are configured", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetOpenConnectionAsync_Throws_WhenDatabaseIsUnknown()
        {
            var factory = CreateFactory("sales", "hr");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => factory.GetOpenConnectionAsync("nope"));

            Assert.Contains("Unknown database 'nope'", ex.Message, StringComparison.Ordinal);
            Assert.Contains("hr, sales", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task GetOpenConnectionAsync_Throws_WhenDatabaseIsOmittedAndSeveralAreConfigured()
        {
            var factory = CreateFactory("sales", "hr");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => factory.GetOpenConnectionAsync());

            Assert.Contains("'database' argument is required", ex.Message, StringComparison.Ordinal);
            Assert.Contains("hr, sales", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task GetOpenConnectionAsync_MatchesTheNameIgnoringCase()
        {
            var factory = CreateFactory("sales", "hr");

            // Name resolution succeeds, so the only failure left is the connection attempt itself.
            var ex = await Record.ExceptionAsync(() => factory.GetOpenConnectionAsync("SALES"));

            Assert.NotNull(ex);
            Assert.DoesNotContain("Unknown database", ex!.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Databases_IncludesDefault_WhenTheLegacyConnectionStringIsSet()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CONNECTION_STRING"] = FakeConnectionString,
                })
                .Build();

            var factory = new SqlConnectionFactory(configuration);

            Assert.Equal(new[] { "default" }, factory.Databases);
        }
    }
}
