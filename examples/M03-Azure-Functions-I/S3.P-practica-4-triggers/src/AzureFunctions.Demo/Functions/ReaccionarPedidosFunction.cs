using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 4/4 — Cosmos DB Change Feed. Reacciona a inserts/updates en
// "pedidos" y los anota al log de notificaciones (consumible desde HTTP
// para verificar que el trigger reaccionó).
public sealed class ReaccionarPedidosFunction
{
    private readonly INotificacionLog _log;
    private readonly ILogger<ReaccionarPedidosFunction> _logger;

    public ReaccionarPedidosFunction(
        INotificacionLog log,
        ILogger<ReaccionarPedidosFunction> logger)
    {
        _log = log;
        _logger = logger;
    }

    [Function(nameof(ReaccionarCambiosPedidos))]
    public void ReaccionarCambiosPedidos(
        [CosmosDBTrigger(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases-practica",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Pedido> cambios)
    {
        Procesar(cambios);
    }

    internal int Procesar(IReadOnlyList<Pedido>? cambios)
    {
        if (cambios is null || cambios.Count == 0) return 0;

        _logger.LogInformation("*** {Count} cambios detectados en pedidos ***", cambios.Count);

        foreach (var pedido in cambios)
        {
            _log.Anotar(pedido);
            _logger.LogInformation(
                "  Pedido {Id}: cliente={Cliente} total={Total:0.00} estado={Estado}",
                pedido.Id, pedido.ClienteId, pedido.Total, pedido.Estado);
        }

        return cambios.Count;
    }
}
