using System.Text.Json.Serialization;
using ClaudeCode.Infra.Demo.Api.Endpoints;
using ClaudeCode.Infra.Demo.Api.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IInfraPlanner, InfraPlanner>();

var app = builder.Build();

app.MapInfra();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
