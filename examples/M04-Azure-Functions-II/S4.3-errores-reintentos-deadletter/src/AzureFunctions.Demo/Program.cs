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

// Servicios de resiliencia (slides 3, 9, 10, 16). Singleton: el circuit
// breaker y el store de idempotencia comparten estado entre invocaciones
// mientras la instancia esté caliente.
builder.Services.AddSingleton<IErrorClassifier, ErrorClassifier>();
builder.Services.AddSingleton<IPoisonClassifier, PoisonClassifier>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<IResilientApiClient, PollyResilientApiClient>();
builder.Services.AddSingleton<IEstadoTracker, InMemoryEstadoTracker>();

builder.Build().Run();
