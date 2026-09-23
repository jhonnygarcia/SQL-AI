// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

/// <summary>
/// Picks which <see cref="Tools"/> methods the server exposes. Each tool's own
/// <see cref="McpServerToolAttribute.ReadOnly"/> hint decides whether it survives read-only mode,
/// so adding a tool still needs no registration.
/// </summary>
public static class ToolRegistry
{
    public static IReadOnlyList<MethodInfo> GetToolMethods(bool readOnly) =>
        typeof(Tools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(method => (method, attribute: method.GetCustomAttribute<McpServerToolAttribute>()))
            .Where(tool => tool.attribute is not null && (!readOnly || tool.attribute.ReadOnly))
            .Select(tool => tool.method)
            .ToList();

    /// <summary>
    /// Wraps a <see cref="Tools"/> method as an MCP tool whose every invocation runs on the
    /// <see cref="Tools"/> singleton from the request's services. Tools has no parameterless
    /// constructor, so the SDK must never be left to instantiate it. The tool keeps the method's
    /// name (e.g. <c>ReadData</c>): the SDK would otherwise publish it as <c>read_data</c>, breaking
    /// client permission rules and prompts written against the original names.
    /// </summary>
    public static McpServerTool CreateTool(MethodInfo method) =>
        McpServerTool.Create(
            method,
            context => context.Services!.GetRequiredService<Tools>(),
            new McpServerToolCreateOptions { Name = method.Name });
}
