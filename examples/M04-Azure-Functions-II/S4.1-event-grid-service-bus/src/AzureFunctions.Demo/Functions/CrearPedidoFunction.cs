using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 13 — HTTP responde rápido (HTTP 202 Accepted) y el ServiceBusOutput
// encola el trabajo pesado. El cliente NO espera al procesamiento.
//
// Multi-output (slide 6 de S3.6 — reutilizado aquí):
//   HttpResponse  → 202 con el id del pedido
//   MensajeCola   → JSON al queue "pedidos-procesar"
//   MensajeTopic  → JSON al topic "pedidos-eventos" (multi-suscriptor)
public sealed class CrearPedidoFunction
{
    private readonly IPedidosOrquestador _orquestador;
    private readonly IEstadoTracker _tracker;
    private readonly ILogger<CrearPedidoFunction> _logger;

    public CrearPedidoFunction(
        IPedidosOrquestador orquestador,
        IEstadoTracker tracker,
        ILogger<CrearPedidoFunction> logger)
    {
        _orquestador = orquestador;
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(CrearPedido))]
    public async Task<CrearPedidoResult> CrearPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos")] HttpRequest req)
    {
        CrearPedidoDto? dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<CrearPedidoDto>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido en POST /pedidos");
            return new CrearPedidoResult
            {
                HttpResponse = new BadRequestObjectResult(new { error = "Body JSON inválido" }),
            };
        }

        var (errores, pedido, mensaje) = _orquestador.ValidarYPreparar(dto);

        if (pedido is null || mensaje is null)
        {
            return new CrearPedidoResult
            {
                HttpResponse = new BadRequestObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Validación falló",
                    status = 400,
                    errors = errores,
                }),
            };
        }

        _tracker.Encolado(pedido.Id);
        _logger.LogInformation("Pedido {Id} encolado a SB", pedido.Id);

        return new CrearPedidoResult
        {
            // 202 Accepted: el trabajo aún no se ha hecho, pero está aceptado
            // y se procesará en background. El cliente puede hacer polling
            // con GET /api/estado para ver el progreso.
            HttpResponse = new AcceptedResult(
                $"/api/estado",
                new { pedidoId = pedido.Id, estado = "encolado" }),
            MensajeCola = mensaje,
            MensajeTopic = mensaje,
        };
    }
}

public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult HttpResponse { get; set; } = null!;

    // Slide 13 — ServiceBus Output binding sobre QUEUE: un solo consumidor.
    [ServiceBusOutput("pedidos-procesar", Connection = "ServiceBusConnection")]
    public string? MensajeCola { get; set; }

    // Slide 13/16 — ServiceBus Output binding sobre TOPIC: N suscriptores.
    [ServiceBusOutput("pedidos-eventos", Connection = "ServiceBusConnection",
        EntityType = ServiceBusEntityType.Topic)]
    public string? MensajeTopic { get; set; }
}
