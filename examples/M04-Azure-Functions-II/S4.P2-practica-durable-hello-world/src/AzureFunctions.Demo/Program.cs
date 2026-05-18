using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Slide 5/15 — Durable Functions no necesita registro propio (basta el
// paquete + atributos). DI cruzado a mano contra cada [Function] (lección
// del bug de S3.4): ninguna inyecta servicios de negocio
//   SaludarActivity / SaludosOrchestrator → sin deps
//   SaludosStarterFunctions → ILogger (auto) + [DurableClient] (binding)
// → no hace falta ningún AddSingleton.

builder.Build().Run();
