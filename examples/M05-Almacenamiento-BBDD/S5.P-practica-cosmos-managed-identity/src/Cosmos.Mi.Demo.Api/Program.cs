using Azure.Core;
using Cosmos.Mi.Demo.Api.Cosmos;
using Cosmos.Mi.Demo.Api.Endpoints;
using Cosmos.Mi.Demo.Api.Repositories;
using Cosmos.Mi.Demo.Api.Security;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Slide 7/8 — SOLO la URL del endpoint, SIN key. Fallback al emulador
// local para desarrollo (slide 9).
var endpoint = cfg["CosmosEndpoint"];
if (string.IsNullOrWhiteSpace(endpoint))
    endpoint = "https://localhost:8081/";

// S5.4 — una credencial singleton compartida (slide 21 de S5.4).
builder.Services.AddSingleton<TokenCredential>(_ => CredentialFactory.Crear(cfg));

// Slide 8 — CosmosClient con DefaultAzureCredential: CERO keys. En local
// usa `az login`; en Azure, la Managed Identity de la app.
builder.Services.AddSingleton(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    return new CosmosClient(endpoint, credential, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
        },
        MaxRetryAttemptsOnRateLimitedRequests = 9,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
    });
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    return client.GetContainer(CosmosDefaults.Database, CosmosDefaults.Container);
});

builder.Services.AddSingleton<IProductoRepository, ProductoRepository>();

var app = builder.Build();

app.MapPractica();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
