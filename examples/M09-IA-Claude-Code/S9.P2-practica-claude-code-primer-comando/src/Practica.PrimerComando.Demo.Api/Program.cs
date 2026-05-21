using System.Text.Json.Serialization;
using Practica.PrimerComando.Demo.Api.Endpoints;
using Practica.PrimerComando.Demo.Api.PrimerComando;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaPrimerComandoPlanner, PracticaPrimerComandoPlanner>();

var app = builder.Build();

app.MapPrimerComando();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
