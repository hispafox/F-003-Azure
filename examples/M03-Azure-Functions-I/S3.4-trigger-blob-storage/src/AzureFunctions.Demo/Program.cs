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

// Slide 25 (S3.1) — services como SINGLETON: misma instancia entre
// ejecuciones, estado en memoria persiste mientras la instancia esté
// caliente. TODOS los servicios que inyectan las funciones por
// constructor deben registrarse aquí o el host no puede instanciarlas:
//   CsvImportFunction     → ICsvProductosImporter, IImportSummaryService
//   ImportsHttpFunctions  → IImportSummaryService
//   InformesHttpFunctions → IInformeService
//   TimerFunctions        → IProductoService, IInformeService
//   ProductosFunctions    → IProductoService
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
builder.Services.AddSingleton<IInformeService, InMemoryInformeService>();
builder.Services.AddSingleton<IImportSummaryService, InMemoryImportSummaryService>();
builder.Services.AddSingleton<ICsvProductosImporter, CsvProductosImporter>();

builder.Build().Run();
