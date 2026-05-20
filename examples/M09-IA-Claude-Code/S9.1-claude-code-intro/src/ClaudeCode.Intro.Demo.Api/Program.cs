using System.Text.Json.Serialization;
using ClaudeCode.Intro.Demo.Api.ClaudeCode;
using ClaudeCode.Intro.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IClaudeCodePlanner, ClaudeCodePlanner>();

var app = builder.Build();

app.MapClaudeCode();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
