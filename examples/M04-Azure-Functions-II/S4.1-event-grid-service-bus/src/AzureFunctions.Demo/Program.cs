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

// 2 singletons compartidos por las 5 funciones:
//   - IPedidosOrquestador: validación + mapeo (testable sin Functions runtime)
//   - IEstadoTracker:      contadores in-memory para inspección desde /estado
builder.Services.AddSingleton<IPedidosOrquestador, PedidosOrquestador>();
builder.Services.AddSingleton<IEstadoTracker, InMemoryEstadoTracker>();

builder.Build().Run();
