using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 9 — Consumidor 2 del Change Feed: materializa un resumen por
// cliente en otro contenedor ("resumenes-clientes") usando un output
// binding. Lease container propio ("leases-resumenes", slide 17) para
// que sea independiente del consumidor de notificaciones.
//
// Slide 10 — el upsert por id="resumen-{clienteId}" es idempotente por
// construcción: reprocesar el mismo batch produce el mismo documento.
public sealed class MaterializarResumenClienteFunction
{
    private readonly IResumenClienteService _espejo;
    private readonly ILogger<MaterializarResumenClienteFunction> _logger;

    public MaterializarResumenClienteFunction(
        IResumenClienteService espejo,
        ILogger<MaterializarResumenClienteFunction> logger)
    {
        _espejo = espejo;
        _logger = logger;
    }

    [Function(nameof(MaterializarResumen))]
    [CosmosDBOutput(
        databaseName: "tienda",
        containerName: "resumenes-clientes",
        Connection = "CosmosDbConnection",
        CreateIfNotExists = true,
        PartitionKey = "/clienteId")]
    public IReadOnlyList<ResumenCliente> MaterializarResumen(
        [CosmosDBTrigger(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases-resumenes",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Pedido> cambios)
    {
        return Procesar(cambios);
    }

    // Handler puro — invocable desde tests. Devuelve los resúmenes que
    // el output binding escribirá a Cosmos, y los duplica en el espejo
    // in-memory para que los endpoints HTTP los puedan listar.
    internal IReadOnlyList<ResumenCliente> Procesar(IReadOnlyList<Pedido>? cambios)
    {
        if (cambios is null || cambios.Count == 0)
        {
            return Array.Empty<ResumenCliente>();
        }

        // Slide 9 — agrupar el batch por cliente.
        var resumenes = cambios
            .Where(p => !string.IsNullOrEmpty(p.ClienteId))
            .GroupBy(p => p.ClienteId)
            .Select(g => new ResumenCliente
            {
                Id = $"resumen-{g.Key}",
                ClienteId = g.Key,
                UltimoPedidoTimestamp = g.Max(p => p.Timestamp),
                TotalPedidos = g.Count(),
                ImporteAcumulado = g.Sum(p => p.Total),
                ActualizadoEn = DateTimeOffset.UtcNow,
            })
            .ToList();

        _espejo.Upsert(resumenes);

        _logger.LogInformation(
            "Materializados {Count} resúmenes a partir de {Cambios} cambios",
            resumenes.Count, cambios.Count);

        return resumenes;
    }
}
