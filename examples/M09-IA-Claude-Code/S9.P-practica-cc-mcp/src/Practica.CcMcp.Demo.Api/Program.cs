using System.Text.Json.Serialization;
using Practica.CcMcp.Demo.Api.Endpoints;
using Practica.CcMcp.Demo.Api.Practica;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaCcMcpPlanner, PracticaCcMcpPlanner>();

var app = builder.Build();

app.MapPractica();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
