using System.Text.Json.Serialization;
using PracticaMsix.Demo.Api.Endpoints;
using PracticaMsix.Demo.Api.Practica;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone PracticaSteps +
// PracticaCertCheck + PracticaArtefactosBuilder en el plan de la
// práctica. Sin estado → singleton.
builder.Services.AddSingleton<IPracticaMsixPlanner, PracticaMsixPlanner>();

var app = builder.Build();

app.MapPractica();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
