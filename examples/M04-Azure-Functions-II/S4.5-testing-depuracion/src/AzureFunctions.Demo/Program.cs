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

// Toda la lógica en servicios → las funciones quedan finas y testables
// (slide 6/7/10/11). Los tests sustituyen estas interfaces.
builder.Services.AddSingleton<IDescuentoCalculator, DescuentoCalculator>();
builder.Services.AddSingleton<ILimpiezaService, InMemoryLimpiezaService>();
builder.Services.AddSingleton<ICsvResumenService, CsvResumenService>();

builder.Build().Run();
