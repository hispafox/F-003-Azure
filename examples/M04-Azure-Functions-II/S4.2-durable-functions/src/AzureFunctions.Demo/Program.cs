using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Slide 5/15 — Durable Functions no necesita registro propio (basta el
// paquete + atributos). Lo que SÍ registramos son los servicios de negocio
// que las activities inyectan. Singleton para que el inventario in-memory
// sobreviva entre invocaciones de activities de la misma instancia.
builder.Services.AddSingleton<IPedidoValidador, PedidoValidador>();
builder.Services.AddSingleton<IInventarioService, InMemoryInventarioService>();
builder.Services.AddSingleton<IPagoService, InMemoryPagoService>();
builder.Services.AddSingleton<INotificacionService, InMemoryNotificacionService>();
builder.Services.AddSingleton<InMemoryFacturacionService>();

builder.Build().Run();
