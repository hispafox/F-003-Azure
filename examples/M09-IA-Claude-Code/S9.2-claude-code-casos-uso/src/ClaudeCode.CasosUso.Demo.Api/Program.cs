using System.Text.Json.Serialization;
using ClaudeCode.CasosUso.Demo.Api.CasosUso;
using ClaudeCode.CasosUso.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<ICasosUsoPlanner, CasosUsoPlanner>();

var app = builder.Build();

app.MapCasosUso();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
