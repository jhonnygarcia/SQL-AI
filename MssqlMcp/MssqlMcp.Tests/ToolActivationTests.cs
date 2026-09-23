// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public sealed class ToolActivationTests
    {
        [Fact]
        public async Task RegisteredTool_IsInvokedOnTheToolsSingleton()
        {
            // No database is contacted: ListDatabases only reads the configured names.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:only"] = "Server=unused;Database=unused",
                })
                .Build();

            var constructed = 0;
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>()
                .AddSingleton(Mock.Of<ILogger<Tools>>())
                .AddSingleton(sp =>
                {
                    constructed++;
                    return new Tools(sp.GetRequiredService<ISqlConnectionFactory>(), sp.GetRequiredService<ILogger<Tools>>());
                })
                .BuildServiceProvider();

            var method = ToolRegistry.GetToolMethods(readOnly: true).Single(m => m.Name == nameof(Tools.ListDatabases));
            var tool = ToolRegistry.CreateTool(method);

            for (var call = 0; call < 3; call++)
            {
                var context = new RequestContext<CallToolRequestParams>(
                    Mock.Of<McpServer>(),
                    new JsonRpcRequest { Method = RequestMethods.ToolsCall, Id = new RequestId(call) },
                    new CallToolRequestParams { Name = tool.ProtocolTool.Name })
                {
                    Services = services,
                };

                var result = await tool.InvokeAsync(context, TestContext.Current.CancellationToken);

                Assert.NotEqual(true, result.IsError);
                var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
                using var json = JsonDocument.Parse(text);
                Assert.True(json.RootElement.GetProperty("success").GetBoolean());
                Assert.Equal("only", json.RootElement.GetProperty("data")[0].GetString());
            }

            // Every call must reuse the DI singleton rather than constructing a Tools per call.
            Assert.Equal(1, constructed);
        }
    }
}
