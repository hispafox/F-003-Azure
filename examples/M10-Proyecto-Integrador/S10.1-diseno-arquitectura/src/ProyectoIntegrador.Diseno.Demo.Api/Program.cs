using System.Text.Json.Serialization;
using ProyectoIntegrador.Diseno.Demo.Api.Diseno;
using ProyectoIntegrador.Diseno.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IProyectoIntegradorPlanner, ProyectoIntegradorPlanner>();

var app = builder.Build();

app.MapDiseno();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
