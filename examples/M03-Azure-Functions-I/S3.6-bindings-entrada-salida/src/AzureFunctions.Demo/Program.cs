using AzureFunctions.Demo.Middleware;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Middlewares (heredados del esqueleto): Correlation-Id + Problem Details.
builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Slide 23 — Patrón híbrido recomendado: el trigger y los bindings son
// declarativos, y la lógica de validación/mapeo va en un servicio
// inyectado. Resultado: funciones cortas y handlers testables sin
// runtime de Functions.
builder.Services.AddSingleton<IPedidosHandler, PedidosHandler>();

builder.Build().Run();
