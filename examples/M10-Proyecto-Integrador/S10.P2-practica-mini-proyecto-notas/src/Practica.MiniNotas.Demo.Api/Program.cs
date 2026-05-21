using System.Text.Json.Serialization;
using Practica.MiniNotas.Demo.Api.Endpoints;
using Practica.MiniNotas.Demo.Api.MiniNotas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaMiniNotasPlanner, PracticaMiniNotasPlanner>();

var app = builder.Build();

app.MapMiniNotas();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
