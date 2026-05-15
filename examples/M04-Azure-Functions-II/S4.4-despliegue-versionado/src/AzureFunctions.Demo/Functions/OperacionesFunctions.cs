using System.Reflection;
using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Endpoints operativos para despliegue seguro:
//   GET  /api/health   → verificación post-deploy (slide 10/17)
//   GET  /api/version  → qué build está vivo + flags activos (slide 14)
//   POST /api/pedidos/procesar → feature-flag switch (slide 16)
public sealed class OperacionesFunctions
{
    private readonly IHealthAggregator _health;
    private readonly IProcesadorSelector _selector;
    private readonly IFeatureFlags _flags;
    private readonly ILogger<OperacionesFunctions> _logger;

    public OperacionesFunctions(
        IHealthAggregator health,
        IProcesadorSelector selector,
        IFeatureFlags flags,
        ILogger<OperacionesFunctions> logger)
    {
        _health = health;
        _selector = selector;
        _flags = flags;
        _logger = logger;
    }

    // Slide 10 — el pipeline llama aquí tras el deploy. 200 = sigue el
    // swap; 503 = aborta / rollback.
    [Function(nameof(Health))]
    public IActionResult Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        var r = _health.Evaluar();
        return r.Estado == "Healthy"
            ? new OkObjectResult(r)
            : new ObjectResult(r) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    }

    // Slide 14 — el script post-deploy compara esta versión con la que
    // esperaba desplegar para confirmar que el deploy "tomó".
    [Function(nameof(Version))]
    public IActionResult Version(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequest req)
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "desconocida";

        return new OkObjectResult(new
        {
            version = info,
            featureFlags = new
            {
                nuevoProcesamiento = _flags.Activo(ProcesadorSelector.Flag),
            },
        });
    }

    [Function(nameof(ProcesarPedido))]
    public async Task<IActionResult> ProcesarPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos/procesar")]
        HttpRequest req)
    {
        Pedido? pedido;
        try
        {
            pedido = await JsonSerializer.DeserializeAsync<Pedido>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Body JSON inválido" });
        }

        if (pedido is null || string.IsNullOrWhiteSpace(pedido.Id))
            return new BadRequestObjectResult(new { error = "Pedido con Id obligatorio" });

        // Slide 16 — el flag decide qué procesador. Sin redeploy para conmutar.
        var procesador = _selector.Seleccionar();
        var resultado = procesador.Procesar(pedido);

        _logger.LogInformation(
            "Pedido {Id} procesado por '{Proc}' → total {Total}",
            resultado.PedidoId, resultado.ProcesadoPor, resultado.Total);

        return new OkObjectResult(resultado);
    }
}
