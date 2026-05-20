using System.Text.Json.Serialization;
using AutoUpdate.Demo.Api.AutoUpdate;
using AutoUpdate.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (CanalDistribucion) — legible en la API.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone AppInstallerBuilder +
// CanaryRolloutPolicy + UpdateVersionAdvisor. Sin estado → singleton.
builder.Services.AddSingleton<IAutoUpdatePlanner, AutoUpdatePlanner>();

var app = builder.Build();

app.MapAutoUpdate();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
