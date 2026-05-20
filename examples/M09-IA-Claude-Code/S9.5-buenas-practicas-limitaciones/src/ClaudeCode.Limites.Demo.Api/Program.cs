using System.Text.Json.Serialization;
using ClaudeCode.Limites.Demo.Api.Endpoints;
using ClaudeCode.Limites.Demo.Api.Limites;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<ILimitesPlanner, LimitesPlanner>();

var app = builder.Build();

app.MapLimites();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
