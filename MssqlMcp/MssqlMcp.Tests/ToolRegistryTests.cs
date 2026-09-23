// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public sealed class ToolRegistryTests
    {
        private static readonly string[] ReadTools = ["DescribeTable", "ListDatabases", "ListTables", "ReadData"];
        private static readonly string[] WriteTools = ["CreateTable", "DropTable", "InsertData", "UpdateData"];

        [Fact]
        public void ReadOnlyMode_ExposesOnlyReadTools()
        {
            var names = ToolRegistry.GetToolMethods(readOnly: true).Select(m => m.Name).Order().ToArray();
            Assert.Equal(ReadTools, names);
        }

        [Fact]
        public void NormalMode_ExposesEveryTool()
        {
            var names = ToolRegistry.GetToolMethods(readOnly: false).Select(m => m.Name).Order().ToArray();
            Assert.Equal(ReadTools.Concat(WriteTools).Order().ToArray(), names);
        }
    }
}
