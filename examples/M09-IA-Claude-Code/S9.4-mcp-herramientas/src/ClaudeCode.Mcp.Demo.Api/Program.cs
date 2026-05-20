using System.Text.Json.Serialization;
using ClaudeCode.Mcp.Demo.Api.Endpoints;
using ClaudeCode.Mcp.Demo.Api.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IMcpPlanner, McpPlanner>();

var app = builder.Build();

app.MapMcp();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
