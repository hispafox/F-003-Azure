using System.Text.Json.Serialization;
using Plataforma.Demo.Api.Endpoints;
using Plataforma.Demo.Api.Plataforma;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPlatformPlanner, PlatformPlanner>();

var app = builder.Build();

app.MapPlataforma();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
