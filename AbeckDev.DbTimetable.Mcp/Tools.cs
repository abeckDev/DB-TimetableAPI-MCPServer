using System;
using System.ComponentModel;
using ModelContextProtocol.Server;


namespace AbeckDev.DbTimetable.Mcp;

[McpServerToolType]
public static class Tools
{
    [McpServerTool, Description("Echoes the message back.")]
    public static string Echo(string message)
    {
        return $"Echo: {message}";
    }
}
