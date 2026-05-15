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

// 3 singletons compartidos por las 4 funciones de la práctica. El
// Function App es UN proceso con 4 triggers; el estado in-memory
// persiste mientras la instancia esté caliente.
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
builder.Services.AddSingleton<ILimpiezaTracker, InMemoryLimpiezaTracker>();
builder.Services.AddSingleton<INotificacionLog, InMemoryNotificacionLog>();

builder.Build().Run();
