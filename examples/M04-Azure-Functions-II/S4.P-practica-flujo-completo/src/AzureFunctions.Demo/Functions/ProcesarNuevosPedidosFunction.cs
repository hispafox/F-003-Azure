using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// PASO 2 — Cosmos Change Feed trigger. Por cada pedido nuevo: genera
// factura → la escribe a Blob y manda el mensaje a Queue (multi-output,
// slide 6 de S3.6).
//
// Slide 11 — idempotencia: el Change Feed es at-least-once. Solo
// facturamos pedidos en estado "nuevo" Y que el tracker no haya marcado
// ya (doble defensa: estado del documento + registro in-memory).
public sealed class ProcesarNuevosPedidosFunction
{
    private readonly IFacturaGenerator _facturas;
    private readonly IFlujoTracker _tracker;
    private readonly ILogger<ProcesarNuevosPedidosFunction> _logger;

    public ProcesarNuevosPedidosFunction(
        IFacturaGenerator facturas,
        IFlujoTracker tracker,
        ILogger<ProcesarNuevosPedidosFunction> logger)
    {
        _facturas = facturas;
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(ProcesarNuevosPedidos))]
    public ProcesarResult ProcesarNuevosPedidos(
        [CosmosDBTrigger(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases-flujo",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Pedido> cambios)
    {
        return Procesar(cambios);
    }

    // Handler puro para tests. Procesa el PRIMER pedido facturable del
    // batch (el binding multi-output escribe un único blob/mensaje por
    // invocación; en un batch grande encadenarías IAsyncCollector, pero
    // para la práctica un pedido por blob es lo claro).
    internal ProcesarResult Procesar(IReadOnlyList<Pedido>? cambios)
    {
        if (cambios is null) return new ProcesarResult();

        foreach (var pedido in cambios)
        {
            if (pedido.Estado != "nuevo")
            {
                _logger.LogDebug("Skip {Id}: estado={Estado}", pedido.Id, pedido.Estado);
                continue;
            }
            if (!_tracker.TryMarcarFacturado(pedido.Id))
            {
                _logger.LogInformation("Skip {Id}: ya facturado (duplicado)", pedido.Id);
                continue;
            }

            var factura = _facturas.Generar(pedido);
            _logger.LogInformation(
                "Factura {Num} generada: {Total}€ con IVA",
                factura.Numero, factura.TotalConIva);

            return new ProcesarResult
            {
                FacturaJson = _facturas.SerializarFactura(factura),
                MensajeCola = _facturas.SerializarMensaje(factura),
            };
        }

        return new ProcesarResult(); // nada facturable en este batch
    }
}

public sealed class ProcesarResult
{
    // El nombre del blob no puede depender de datos del documento aquí
    // (no hay binding expression sobre el trigger de Cosmos), así que
    // usamos {rand-guid}; el pedidoId va dentro del JSON.
    [BlobOutput("facturas/{rand-guid}.json", Connection = "AzureWebJobsStorage")]
    public string? FacturaJson { get; set; }

    [QueueOutput("facturas-generadas", Connection = "AzureWebJobsStorage")]
    public string? MensajeCola { get; set; }
}
