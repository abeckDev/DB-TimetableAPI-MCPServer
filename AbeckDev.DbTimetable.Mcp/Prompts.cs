using System;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace AbeckDev.DbTimetable.Mcp;

[McpServerPromptType]
public static class Prompts
{
   [McpServerPrompt, Description("Creates a prompt to summarize the provided message.")]
   public static ChatMessage Summarize([Description("The content to summarize")] string content) =>
        new(ChatRole.User, $"Please summarize this content into a single sentence: {content}");
}
