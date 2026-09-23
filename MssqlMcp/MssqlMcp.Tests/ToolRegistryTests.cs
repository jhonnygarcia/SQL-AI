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
        public void CreatedTools_KeepMethodNamesAndHints()
        {
            foreach (var method in ToolRegistry.GetToolMethods(readOnly: false))
            {
                var tool = ToolRegistry.CreateTool(method).ProtocolTool;
                Assert.Equal(method.Name, tool.Name);

                // Setting the name through create options must not drop the attribute's hints.
                var readOnly = method.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false)
                    .Cast<ModelContextProtocol.Server.McpServerToolAttribute>().Single().ReadOnly;
                Assert.Equal(readOnly, tool.Annotations?.ReadOnlyHint);
                Assert.False(string.IsNullOrWhiteSpace(tool.Title));
            }
        }

        [Fact]
        public void NormalMode_ExposesEveryTool()
        {
            var names = ToolRegistry.GetToolMethods(readOnly: false).Select(m => m.Name).Order().ToArray();
            Assert.Equal(ReadTools.Concat(WriteTools).Order().ToArray(), names);
        }
    }
}
