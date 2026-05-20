using System.Text.Json.Serialization;
using Pipelines.Demo.Api.Endpoints;
using Pipelines.Demo.Api.Pipelines;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPipelinePlanner, PipelinePlanner>();

var app = builder.Build();

app.MapPipelines();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
