using System.Text.Json.Serialization;
using Practica.Pipeline.Demo.Api.Endpoints;
using Practica.Pipeline.Demo.Api.Pipeline;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaPipelinePlanner, PracticaPipelinePlanner>();

var app = builder.Build();

app.MapPipeline();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
