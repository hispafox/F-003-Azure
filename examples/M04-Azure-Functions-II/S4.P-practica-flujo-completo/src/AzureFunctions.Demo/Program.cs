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

// DI — cruzado a mano contra cada constructor de [Function] (la lección
// del bug de S3.4): registrar TODO lo que inyectan o el host no puede
// instanciar las funciones (los tests no lo detectan).
//   CrearPedidoFunction          → IPedidoFactory, IFlujoTracker
//   ProcesarNuevosPedidosFunction→ IFacturaGenerator, IFlujoTracker
//   NotificarFacturaFunction     → IFlujoTracker
//   EstadoFunction               → IFlujoTracker
// IFlujoTracker es Singleton: estado compartido entre los 3 saltos del
// flujo + el endpoint de inspección.
builder.Services.AddSingleton<IPedidoFactory, PedidoFactory>();
builder.Services.AddSingleton<IFacturaGenerator, FacturaGenerator>();
builder.Services.AddSingleton<IFlujoTracker, InMemoryFlujoTracker>();

builder.Build().Run();
