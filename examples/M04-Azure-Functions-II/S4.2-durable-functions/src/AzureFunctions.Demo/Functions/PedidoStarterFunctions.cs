using System.Text.Json;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 11 — Starters: las funciones HTTP que arrancan / consultan /
// señalizan orquestaciones. NO son orquestadores: aquí SÍ se puede hacer
// I/O y usar el DurableTaskClient.
public sealed class PedidoStarterFunctions
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<PedidoStarterFunctions> _logger;

    public PedidoStarterFunctions(ILogger<PedidoStarterFunctions> logger)
    {
        _logger = logger;
    }

    // POST /api/pedidos/procesar → arranca ProcesarPedido
    [Function(nameof(IniciarPedido))]
    public async Task<IActionResult> IniciarPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos/procesar")]
        HttpRequest req,
        [DurableClient] DurableTaskClient client)
    {
        Pedido? pedido;
        try
        {
            pedido = await JsonSerializer.DeserializeAsync<Pedido>(req.Body, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido");
            return new BadRequestObjectResult(new { error = "Body JSON inválido" });
        }

        if (pedido is null || string.IsNullOrWhiteSpace(pedido.Id))
            return new BadRequestObjectResult(new { error = "Pedido con Id es obligatorio" });

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProcesarPedidoOrchestrator.ProcesarPedido), pedido);

        _logger.LogInformation("Orquestación {Instance} iniciada para pedido {Id}",
            instanceId, pedido.Id);

        return new AcceptedResult(
            $"/api/pedidos/estado/{instanceId}",
            new
            {
                instanceId,
                estadoUrl = $"/api/pedidos/estado/{instanceId}",
                aprobarUrl = $"/api/pedidos/{instanceId}/aprobar",
            });
    }

    // GET /api/pedidos/estado/{instanceId} → consulta el estado
    [Function(nameof(EstadoPedido))]
    public async Task<IActionResult> EstadoPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pedidos/estado/{instanceId}")]
        HttpRequest req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var estado = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);
        if (estado is null)
            return new NotFoundObjectResult(new { error = $"Instancia {instanceId} no encontrada" });

        return new OkObjectResult(new
        {
            instanceId,
            runtimeStatus = estado.RuntimeStatus.ToString(),
            customStatus = estado.SerializedCustomStatus,
            createdAt = estado.CreatedAt,
            lastUpdatedAt = estado.LastUpdatedAt,
            output = estado.SerializedOutput,
        });
    }

    // POST /api/pedidos/{instanceId}/aprobar → manda el evento humano (slide 9)
    [Function(nameof(AprobarPedido))]
    public async Task<IActionResult> AprobarPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos/{instanceId}/aprobar")]
        HttpRequest req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        // El body opcional {"aprobado":true|false}; por defecto true.
        var aprobado = true;
        try
        {
            if (req.ContentLength is > 0)
            {
                var dto = await JsonSerializer.DeserializeAsync<AprobacionDto>(req.Body, JsonOpts);
                aprobado = dto?.Aprobado ?? true;
            }
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Body JSON inválido" });
        }

        var estado = await client.GetInstanceAsync(instanceId);
        if (estado is null)
            return new NotFoundObjectResult(new { error = $"Instancia {instanceId} no encontrada" });

        await client.RaiseEventAsync(
            instanceId, ProcesarPedidoOrchestrator.EventoAprobacion, aprobado);

        return new OkObjectResult(new { instanceId, aprobado, mensaje = "Evento enviado" });
    }

    // POST /api/facturas/lote → arranca el fan-out/fan-in
    [Function(nameof(IniciarLoteFacturas))]
    public async Task<IActionResult> IniciarLoteFacturas(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "facturas/lote")]
        HttpRequest req,
        [DurableClient] DurableTaskClient client)
    {
        List<Factura>? facturas;
        try
        {
            facturas = await JsonSerializer.DeserializeAsync<List<Factura>>(req.Body, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido");
            return new BadRequestObjectResult(new { error = "Body JSON inválido (se espera un array de facturas)" });
        }

        if (facturas is null || facturas.Count == 0)
            return new BadRequestObjectResult(new { error = "El array de facturas no puede estar vacío" });

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ProcesarLoteFacturasOrchestrator.ProcesarLoteFacturas), facturas);

        return new AcceptedResult(
            $"/api/pedidos/estado/{instanceId}",
            new { instanceId, total = facturas.Count });
    }
}

public sealed record AprobacionDto(bool Aprobado);
