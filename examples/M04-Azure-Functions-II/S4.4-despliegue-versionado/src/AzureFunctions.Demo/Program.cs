using AzureFunctions.Demo.Middleware;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IProductoCatalogo, InMemoryProductoCatalogo>();
builder.Services.AddSingleton<IFeatureFlags, EnvFeatureFlags>();

// Procesadores: ambos registrados; el selector elige según el flag.
builder.Services.AddSingleton<ProcesadorLegacy>();
builder.Services.AddSingleton<ProcesadorNuevo>();
builder.Services.AddSingleton<IProcesadorSelector, ProcesadorSelector>();

// Health: los checks se descubren por DI (IEnumerable<IHealthCheck>).
builder.Services.AddSingleton<IHealthCheck, CatalogoHealthCheck>();
builder.Services.AddSingleton<IHealthAggregator, HealthAggregator>();

builder.Build().Run();
