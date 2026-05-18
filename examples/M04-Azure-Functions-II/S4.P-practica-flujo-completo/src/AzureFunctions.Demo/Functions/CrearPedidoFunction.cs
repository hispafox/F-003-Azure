using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// PASO 1 del flujo — HTTP → Cosmos (output binding). El cliente recibe
// 201 al instante; el resto del flujo ocurre en background vía el
// Change Feed.
public sealed class CrearPedidoFunction
{
    private readonly IPedidoFactory _factory;
    private readonly IFlujoTracker _tracker;
    private readonly ILogger<CrearPedidoFunction> _logger;

    public CrearPedidoFunction(
        IPedidoFactory factory,
        IFlujoTracker tracker,
        ILogger<CrearPedidoFunction> logger)
    {
        _factory = factory;
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
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return new CrearPedidoResult
            {
                Http = new BadRequestObjectResult(new { error = "Body JSON inválido" }),
            };
        }

        var (errores, pedido) = _factory.Crear(dto);
        if (pedido is null)
        {
            return new CrearPedidoResult
            {
                Http = new BadRequestObjectResult(new { errores }),
            };
        }

        _tracker.PedidoCreado(pedido.Id);
        _logger.LogInformation(
            "Pedido {Id} creado: {Total}€, {Items} items",
            pedido.Id, pedido.Total, pedido.Items.Count);

        return new CrearPedidoResult
        {
            Http = new CreatedResult($"/api/pedidos/{pedido.Id}", new
            {
                pedido.Id, pedido.Total, estado = pedido.Estado,
            }),
            // El documento va directo a Cosmos; el Change Feed lo recogerá.
            PedidoCosmos = pedido,
        };
    }
}

public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult Http { get; set; } = null!;

    [CosmosDBOutput(
        databaseName: "tienda",
        containerName: "pedidos",
        Connection = "CosmosDbConnection",
        CreateIfNotExists = false,
        PartitionKey = "/clienteId")]
    public Pedido? PedidoCosmos { get; set; }
}
