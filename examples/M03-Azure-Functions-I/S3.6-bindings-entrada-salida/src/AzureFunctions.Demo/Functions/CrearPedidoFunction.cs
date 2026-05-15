using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 6 — Multi-output pattern: una sola función produce 3 efectos:
//   1) Response HTTP al cliente
//   2) Escritura en Cosmos DB (vía [CosmosDBOutput])
//   3) Mensaje a una Queue Storage (vía [QueueOutput])
//
// Los outputs se ejecutan SOLO si las propiedades no son null (slide 24
// — validar antes para no escribir basura). Si el validador falla, el
// HttpResponse lleva 400 + Problem Details y los otros dos outputs son
// null, así que no se materializan.
public sealed class CrearPedidoFunction
{
    private readonly IPedidosHandler _handler;
    private readonly ILogger<CrearPedidoFunction> _logger;

    public CrearPedidoFunction(IPedidosHandler handler, ILogger<CrearPedidoFunction> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [Function(nameof(CrearPedido))]
    public async Task<CrearPedidoResult> CrearPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos")] HttpRequest req)
    {
        CrearPedidoDto? dto;
        try
        {
            // Slide 21 anti-pattern aware: si el JSON es malformado,
            // capturamos aquí y devolvemos 400. Si dejamos que Functions
            // deserialice como parámetro, un body inválido provocaría una
            // excepción opaca antes de entrar al método.
            dto = await JsonSerializer.DeserializeAsync<CrearPedidoDto>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido en POST /pedidos");
            return new CrearPedidoResult
            {
                HttpResponse = new BadRequestObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Body JSON inválido",
                    status = 400,
                    detail = ex.Message,
                }),
            };
        }

        var (errores, pedido) = _handler.ValidarYConstruir(dto);

        if (pedido is null)
        {
            _logger.LogInformation("Validación falló: {Errores}", errores.Count);
            return new CrearPedidoResult
            {
                HttpResponse = new BadRequestObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Errores de validación",
                    status = 400,
                    errors = errores.ToDictionary(
                        e => e.Campo,
                        e => new[] { e.Mensaje }),
                }),
            };
        }

        _logger.LogInformation("Pedido {Id} validado, enviando a Cosmos + Queue", pedido.Id);

        return new CrearPedidoResult
        {
            // El return value entero se inspecciona: las propiedades con
            // atributo de output binding se materializan a sus destinos.
            HttpResponse = new CreatedResult($"/api/pedidos/{pedido.ClienteId}/{pedido.Id}", pedido),
            PedidoCosmos = pedido,
            MensajeCola = JsonSerializer.Serialize(new
            {
                pedidoId = pedido.Id,
                clienteId = pedido.ClienteId,
                total = pedido.Total,
                encolado = DateTimeOffset.UtcNow,
            }),
        };
    }
}

// Slide 6 — Multi-output: un POCO con propiedades anotadas. Cada
// propiedad con atributo de binding se escribe a su destino al return.
// La propiedad HttpResponse (con [HttpResult] implícito o explícito)
// es la respuesta al cliente.
public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult HttpResponse { get; set; } = null!;

    [CosmosDBOutput(
        databaseName: "tienda",
        containerName: "pedidos",
        Connection = "CosmosDbConnection",
        CreateIfNotExists = false,
        PartitionKey = "/clienteId")]
    public Pedido? PedidoCosmos { get; set; }

    [QueueOutput("pedidos-pendientes", Connection = "AzureWebJobsStorage")]
    public string? MensajeCola { get; set; }
}
