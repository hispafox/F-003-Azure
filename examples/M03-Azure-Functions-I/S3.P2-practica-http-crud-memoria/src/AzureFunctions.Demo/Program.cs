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

// Slide 6 — Singleton: una única instancia compartida entre invocaciones.
// Los datos persisten mientras la function esté caliente; cuando se
// duerme, se restauran los del seed (limitación deliberada del CRUD
// en memoria, slide 12 → para persistencia real usar Cosmos en M05).
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();

builder.Build().Run();
