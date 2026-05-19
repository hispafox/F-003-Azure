using System.Text.Json.Serialization;
using Apim.Demo.Api.Apim;
using Apim.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (ApimTier / EsquemaVersionado) — legible y permite
// el binding desde query/body.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone ApimTierAdvisor +
// ApimVersioningResolver + ApimPolicyEvaluator en el plan de APIM.
// Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IApimPlanner, ApimPlanner>();

var app = builder.Build();

app.MapApim();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
