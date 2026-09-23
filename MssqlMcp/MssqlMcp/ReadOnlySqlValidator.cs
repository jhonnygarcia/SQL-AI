// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Mssql.McpServer;

/// <summary>
/// Decides whether a SQL batch only reads data. ReadData advertises itself as read-only, so clients
/// may run it without asking; anything that could change state must be rejected before it reaches
/// the server.
/// </summary>
public static class ReadOnlySqlValidator
{
    public static bool IsReadOnlyQuery(string sql, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            reason = "The query is empty.";
            return false;
        }

        var parser = new TSql170Parser(initialQuotedIdentifiers: true);
        TSqlFragment fragment;
        IList<ParseError> errors;
        using (var reader = new StringReader(sql))
        {
            fragment = parser.Parse(reader, out errors);
        }

        if (errors.Count > 0)
        {
            reason = $"Incorrect syntax: {errors[0].Message} (line {errors[0].Line}, column {errors[0].Column}).";
            return false;
        }

        if (fragment is not TSqlScript script)
        {
            reason = "The query could not be parsed.";
            return false;
        }

        var statements = script.Batches.SelectMany(batch => batch.Statements).ToList();
        if (statements.Count == 0)
        {
            reason = "The query contains no statements.";
            return false;
        }

        foreach (var statement in statements)
        {
            if (statement is not SelectStatement select)
            {
                reason = $"Only SELECT queries are allowed; found {Describe(statement)}.";
                return false;
            }

            if (select.Into is not null)
            {
                reason = "Only SELECT queries are allowed; SELECT ... INTO creates a table.";
                return false;
            }
        }

        // A SELECT can still reach outside the query: OPENQUERY/OPENROWSET/OPENDATASOURCE run
        // arbitrary text on another server, and NEXT VALUE FOR advances a sequence.
        var visitor = new SideEffectVisitor();
        script.Accept(visitor);
        if (visitor.Found is not null)
        {
            reason = $"Only SELECT queries are allowed; {visitor.Found} is not permitted.";
            return false;
        }

        reason = null;
        return true;
    }

    private static string Describe(TSqlStatement statement)
    {
        var name = statement.GetType().Name;
        if (!name.EndsWith("Statement", StringComparison.Ordinal))
        {
            return name;
        }

        var kind = name[..^"Statement".Length];
        var article = "AEIOU".Contains(kind[0]) ? "an" : "a";
        return $"{article} {kind} statement";
    }

    private sealed class SideEffectVisitor : TSqlFragmentVisitor
    {
        public string? Found { get; private set; }

        public override void Visit(OpenQueryTableReference node) => Found ??= "OPENQUERY";

        public override void Visit(OpenRowsetTableReference node) => Found ??= "OPENROWSET";

        public override void Visit(BulkOpenRowset node) => Found ??= "OPENROWSET(BULK ...)";

        public override void Visit(AdHocTableReference node) => Found ??= "OPENDATASOURCE";

        public override void Visit(NextValueForExpression node) => Found ??= "NEXT VALUE FOR";
    }
}
