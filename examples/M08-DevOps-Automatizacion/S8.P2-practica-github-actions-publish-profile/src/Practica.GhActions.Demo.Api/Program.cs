using System.Text.Json.Serialization;
using Practica.GhActions.Demo.Api.Endpoints;
using Practica.GhActions.Demo.Api.GhActions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaGhActionsPlanner, PracticaGhActionsPlanner>();

var app = builder.Build();

app.MapGhActions();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
