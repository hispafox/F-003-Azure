using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 8 — Consumidor 1 del Change Feed: notificaciones por cambio de
// estado del pedido. Lease container propio ("leases-notificaciones",
// slide 5/17) para que sea independiente del consumidor 2.
//
// Slide 10 — at-least-once: la función puede recibir el mismo pedido
// dos veces. INotificacionService.EnviarSiNoEnviada es idempotente por
// construcción (clave PedidoId+Estado).
//
// Slide 12 — manejo de errores: capturamos por pedido para no abortar
// el batch entero cuando falla uno solo.
public sealed class NotificacionesPedidoFunction
{
    private readonly INotificacionService _notificaciones;
    private readonly ILogger<NotificacionesPedidoFunction> _logger;

    public NotificacionesPedidoFunction(
        INotificacionService notificaciones,
        ILogger<NotificacionesPedidoFunction> logger)
    {
        _notificaciones = notificaciones;
        _logger = logger;
    }

    [Function(nameof(NotificarCambioPedido))]
    public void NotificarCambioPedido(
        [CosmosDBTrigger(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases-notificaciones",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Pedido> cambios)
    {
        Procesar(cambios);
    }

    // Handler puro — invocable desde tests sin runtime de Functions.
    // El método público con el atributo solo delega aquí.
    internal int Procesar(IReadOnlyList<Pedido>? cambios)
    {
        if (cambios is null || cambios.Count == 0)
        {
            _logger.LogDebug("Batch vacío, nada que notificar");
            return 0;
        }

        var enviadas = 0;

        foreach (var pedido in cambios)
        {
            try
            {
                var mensaje = MensajePorEstado(pedido);
                if (mensaje is null) continue;

                if (_notificaciones.EnviarSiNoEnviada(pedido.Id, pedido.ClienteId, pedido.Estado, mensaje))
                {
                    enviadas++;
                    _logger.LogInformation(
                        "Notificación enviada: pedido={PedidoId} cliente={ClienteId} estado={Estado}",
                        pedido.Id, pedido.ClienteId, pedido.Estado);
                }
                else
                {
                    // Slide 10 — el segundo intento del mismo cambio cae aquí.
                    _logger.LogDebug(
                        "Notificación ya enviada (idempotencia): pedido={PedidoId} estado={Estado}",
                        pedido.Id, pedido.Estado);
                }
            }
            catch (Exception ex)
            {
                // Slide 12 — tragar el error de un pedido NO aborta el batch.
                // En producción, IDs fallidos irían a un dead-letter.
                _logger.LogError(ex, "Error notificando pedido {PedidoId}", pedido.Id);
            }
        }

        _logger.LogInformation(
            "Batch procesado: {Enviadas}/{Total} notificaciones nuevas",
            enviadas, cambios.Count);

        return enviadas;
    }

    private static string? MensajePorEstado(Pedido p) => p.Estado?.ToLowerInvariant() switch
    {
        "confirmado" => $"Tu pedido {p.Id} ha sido confirmado. Total: {p.Total:0.00} €",
        "enviado" => $"Tu pedido {p.Id} está en camino",
        "entregado" => $"Tu pedido {p.Id} ha sido entregado. ¡Gracias!",
        "cancelado" => $"Tu pedido {p.Id} ha sido cancelado",
        _ => null, // estados intermedios (pendiente, en-preparacion...) no notifican
    };
}
