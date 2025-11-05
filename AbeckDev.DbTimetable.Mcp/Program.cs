//Minimal API approach
using AbeckDev.DbTimetable.Mcp.Models;
using AbeckDev.DbTimetable.Mcp.Services;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

//Setup Config provider
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.Configure<Configuration>(
    builder.Configuration.GetSection(Configuration.SectionName));

var dbConfig = builder.Configuration
    .GetSection(Configuration.SectionName)
    .Get<Configuration>() ?? new Configuration();

builder.Services.AddHttpClient<ITimeTableService, TimeTableService>(client =>
{
    client.BaseAddress = new Uri(dbConfig.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "Deutsche Bahn - Timetable API", Version = "1.0.0" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();


var app = builder.Build();

app.MapMcp("/mcp");

app.Run("http://0.0.0.0:3001");