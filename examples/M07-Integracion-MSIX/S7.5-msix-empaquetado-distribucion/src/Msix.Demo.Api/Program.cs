using System.Text.Json.Serialization;
using Msix.Demo.Api.Endpoints;
using Msix.Demo.Api.Msix;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (CanalDistribucion) — legible en la API.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone AppxManifestValidator +
// PackageNamingResolver + DistributionChannelAdvisor en el plan. Sin
// estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IMsixPackagingPlanner, MsixPackagingPlanner>();

var app = builder.Build();

app.MapMsix();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
