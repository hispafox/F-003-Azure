using AzureFunctions.Demo.Configuration;
using AzureFunctions.Demo.Middleware;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Slide 14 — Middlewares en orden:
//   CorrelationIdMiddleware genera/propaga X-Correlation-Id
//   ExceptionHandlingMiddleware lo envuelve para que cualquier exception
//   no controlada salga como Problem Details con traceId asociado.
builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Slide 13 — DI por constructor + Options pattern.
builder.Services
    .AddOptions<ProductosOptions>()
    .Bind(builder.Configuration.GetSection(ProductosOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Slide 25 (S3.1) — service como SINGLETON: misma instancia entre ejecuciones,
// estado en memoria persiste mientras la instancia esté caliente.
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();

builder.Build().Run();
