using AzureFunctions.Demo.Middleware;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Middlewares (heredados del esqueleto del módulo): Correlation-Id +
// manejo de excepciones a Problem Details.
builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Slide 5 — Los dos consumidores comparten el mismo Change Feed pero
// tienen lease containers distintos ("leases-notificaciones" y
// "leases-resumenes"). Cada servicio mantiene su propio estado:
//
//   INotificacionService     → idempotencia (slide 10)
//   IResumenClienteService   → espejo del output binding (slide 9)
builder.Services.AddSingleton<INotificacionService, InMemoryNotificacionService>();
builder.Services.AddSingleton<IResumenClienteService, InMemoryResumenClienteService>();

builder.Build().Run();
