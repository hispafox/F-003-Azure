using Cosmos.Demo.Api.Cosmos;
using Cosmos.Demo.Api.Endpoints;
using Cosmos.Demo.Api.Repositories;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Slide 15 — CosmosClient SIEMPRE singleton (gestiona su propio pool de
// conexiones; crear uno por petición es un error de rendimiento grave).
builder.Services.AddSingleton(_ =>
{
    // Sin cadena configurada → emulador local (clave pública, no secreto).
    var cs = cfg["CosmosDbConnection"];
    if (string.IsNullOrWhiteSpace(cs))
        cs = CosmosDefaults.EmuladorConnectionString;

    var options = new CosmosClientOptions
    {
        // Slide 9 — camelCase: Id→"id" (lo exige Cosmos), ClienteId→"clienteId".
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
        },
        ConnectionMode = ConnectionMode.Direct, // más rápido que Gateway
        // Slide 32 (anti-pattern 5) — reintentar throttling (429).
        MaxRetryAttemptsOnRateLimitedRequests = 9,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
    };

    // Slide 11 — la consistencia se puede DEBILITAR desde el cliente
    // (nunca reforzar por encima de la de la cuenta). Opcional.
    var consistencia = cfg["CosmosConsistency"];
    if (!string.IsNullOrWhiteSpace(consistencia) &&
        Enum.TryParse<ConsistencyLevel>(consistencia, ignoreCase: true, out var nivel))
    {
        options.ConsistencyLevel = nivel;
    }

    return new CosmosClient(cs, options);
});

// Slide 15 — el Container como servicio (derivado del client singleton).
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var db = cfg["CosmosDatabase"] ?? CosmosDefaults.Database;
    var container = cfg["CosmosContainer"] ?? CosmosDefaults.Container;
    return client.GetContainer(db, container);
});

// El repo solo depende del Container singleton → singleton también.
builder.Services.AddSingleton<IPedidoRepository, PedidoRepository>();

var app = builder.Build();

app.MapPedidos();

// El bootstrap del database/container (CreateIfNotExists) lo hace el
// script de provisión / el test de integración explícitamente, NO el
// arranque de la app (mismo criterio que las migraciones en S5.2).

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
