using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Slide 12 — Patron canonico de un Function App isolated worker:
// HostBuilder + ConfigureFunctionsWebApplication + DI igual que en ASP.NET Core.
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Slides 11 y 23 — Application Insights (telemetria) registrado siempre que
// haya APPLICATIONINSIGHTS_CONNECTION_STRING en el entorno. Sin ella el
// codigo arranca igual; solo se pierde la telemetria en Azure.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Aqui se registrarian servicios y HttpClients que usen las funciones.
// Lo dejamos vacio porque S3.1 es introductorio; en S3.2 ya empezamos a usarlo.

builder.Build().Run();
