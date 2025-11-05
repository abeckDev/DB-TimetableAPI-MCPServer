//Minimal API approach
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "Deutsche Bahn - Timetable API", Version = "" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();


var app = builder.Build();
app.MapMcp("/mcp");

app.Run("http://localhost:3001");