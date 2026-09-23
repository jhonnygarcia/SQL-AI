// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Reflection;
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
}
