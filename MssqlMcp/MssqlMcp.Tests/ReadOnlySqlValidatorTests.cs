// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public sealed class ReadOnlySqlValidatorTests
    {
        [Theory]
        [InlineData("SELECT 1")]
        [InlineData("SELECT name FROM sys.tables WHERE name LIKE 'A%' ORDER BY name")]
        [InlineData("WITH c AS (SELECT 1 AS Id) SELECT Id FROM c")]
        [InlineData("SELECT 1 AS Id UNION ALL SELECT 2")]
        [InlineData("-- the latest rows\nSELECT TOP 5 * FROM sys.objects /* inline */ ORDER BY create_date DESC")]
        [InlineData("SELECT 1; SELECT 2")]
        [InlineData("SELECT name FROM sys.tables FOR JSON PATH")]
        public void Accepts_PlainSelects(string sql)
        {
            Assert.True(ReadOnlySqlValidator.IsReadOnlyQuery(sql, out var reason), reason);
            Assert.Null(reason);
        }

        [Theory]
        [InlineData("INSERT INTO t (Id) VALUES (1)")]
        [InlineData("UPDATE t SET Id = 2")]
        [InlineData("DELETE FROM t")]
        [InlineData("MERGE t AS target USING s ON target.Id = s.Id WHEN MATCHED THEN DELETE;")]
        [InlineData("TRUNCATE TABLE t")]
        [InlineData("DROP TABLE t")]
        [InlineData("CREATE TABLE t (Id INT)")]
        [InlineData("ALTER TABLE t ADD Name NVARCHAR(10)")]
        [InlineData("EXEC sp_who")]
        [InlineData("EXECUTE('DELETE FROM t')")]
        [InlineData("DECLARE @x INT = 1")]
        [InlineData("SELECT * INTO t2 FROM t")]
        [InlineData("SELECT 1; DELETE FROM t")]
        [InlineData("SELECT 1\nGO\nDROP TABLE t")]
        [InlineData("WITH c AS (SELECT * FROM t) DELETE FROM c")]
        [InlineData("SELECT * FROM OPENQUERY(linked, 'DELETE FROM t')")]
        [InlineData("SELECT * FROM OPENROWSET('SQLNCLI', 'Server=x;Trusted_Connection=yes;', 'SELECT 1')")]
        [InlineData("SELECT * FROM OPENROWSET(BULK 'C:\\secret.txt', SINGLE_CLOB) AS f")]
        [InlineData("SELECT * FROM OPENDATASOURCE('SQLNCLI', 'Data Source=x').db.dbo.t")]
        [InlineData("SELECT NEXT VALUE FOR dbo.seq")]
        [InlineData("SELECT FROM")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("-- just a comment")]
        public void Rejects_AnythingThatCouldWrite(string sql)
        {
            Assert.False(ReadOnlySqlValidator.IsReadOnlyQuery(sql, out var reason));
            Assert.False(string.IsNullOrWhiteSpace(reason));
        }

        [Fact]
        public void Rejection_NamesTheOffendingStatement()
        {
            Assert.False(ReadOnlySqlValidator.IsReadOnlyQuery("SELECT 1; DELETE FROM t", out var reason));
            Assert.Contains("Delete", reason ?? string.Empty, StringComparison.Ordinal);
        }

        [Fact]
        public void SyntaxErrors_MentionSyntax()
        {
            Assert.False(ReadOnlySqlValidator.IsReadOnlyQuery("SELECT FROM", out var reason));
            Assert.Contains("syntax", reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
