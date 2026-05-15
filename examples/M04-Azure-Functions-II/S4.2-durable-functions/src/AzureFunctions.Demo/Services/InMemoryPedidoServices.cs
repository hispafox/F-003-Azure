using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Services;

public sealed class PedidoValidador : IPedidoValidador
{
    public void Validar(Pedido pedido)
    {
        if (pedido is null)
            throw new InvalidOperationException("Pedido nulo");
        if (string.IsNullOrWhiteSpace(pedido.ClienteId))
            throw new InvalidOperationException("ClienteId obligatorio");
        if (pedido.Items is null || pedido.Items.Count == 0)
            throw new InvalidOperationException("El pedido no tiene líneas");
        if (pedido.Total <= 0)
            throw new InvalidOperationException("Total debe ser mayor que 0");
    }
}

// Reserva de inventario in-memory. Liberar() es la compensación (saga).
public sealed class InMemoryInventarioService(ILogger<InMemoryInventarioService> logger)
    : IInventarioService
{
    private readonly ConcurrentDictionary<string, Reserva> _reservas = new();

    public Reserva Reservar(Pedido pedido)
    {
        var reserva = new Reserva(
            ReservaId: $"rsv-{Guid.NewGuid():N}"[..12],
            PedidoId: pedido.Id,
            Confirmada: true);
        _reservas[reserva.ReservaId] = reserva;
        logger.LogInformation("Inventario reservado {ReservaId} para pedido {PedidoId}",
            reserva.ReservaId, pedido.Id);
        return reserva;
    }

    public void Liberar(string reservaId)
    {
        if (_reservas.TryRemove(reservaId, out _))
            logger.LogInformation("Inventario liberado (compensación): {ReservaId}", reservaId);
        else
            logger.LogWarning("Liberar: reserva {ReservaId} no encontrada (idempotente)", reservaId);
    }

    // Solo para tests/inspección.
    public bool ExisteReserva(string reservaId) => _reservas.ContainsKey(reservaId);
}

// El pago falla de forma determinista si el total termina en .99 — sirve
// para forzar el camino de compensación (saga) en demos y tests.
public sealed class InMemoryPagoService(ILogger<InMemoryPagoService> logger) : IPagoService
{
    public Pago Cobrar(Pedido pedido, Reserva reserva)
    {
        if (!reserva.Confirmada)
            throw new PagoRechazadoException("La reserva no está confirmada");

        var centimos = (int)(pedido.Total * 100) % 100;
        if (centimos == 99)
        {
            logger.LogWarning("Pago RECHAZADO para pedido {PedidoId} (total .99)", pedido.Id);
            throw new PagoRechazadoException(
                $"Tarjeta rechazada para el importe {pedido.Total:0.00}");
        }

        var pago = new Pago(
            TransaccionId: $"txn-{Guid.NewGuid():N}"[..14],
            PedidoId: pedido.Id,
            Importe: pedido.Total,
            Exito: true);
        logger.LogInformation("Pago OK {TxnId} pedido {PedidoId}",
            pago.TransaccionId, pedido.Id);
        return pago;
    }
}

public sealed class InMemoryNotificacionService(ILogger<InMemoryNotificacionService> logger)
    : INotificacionService
{
    public void EnviarConfirmacion(Pedido pedido, Pago pago) =>
        logger.LogInformation("Confirmación enviada a {Email}: pedido {Id} pago {Txn}",
            pedido.ClienteEmail, pedido.Id, pago.TransaccionId);

    public void NotificarManager(Pedido pedido) =>
        logger.LogInformation("Manager notificado: pedido {Id} importe {Total} requiere aprobación",
            pedido.Id, pedido.Total);

    public void NotificarRechazo(Pedido pedido, string motivo) =>
        logger.LogInformation("Rechazo notificado a {Email}: pedido {Id} — {Motivo}",
            pedido.ClienteEmail, pedido.Id, motivo);
}

// Procesa una factura individual (la unidad del fan-out, slide 7).
public sealed class InMemoryFacturacionService(ILogger<InMemoryFacturacionService> logger)
{
    public ResultadoFactura Procesar(Factura factura)
    {
        // Las facturas de importe 0 o negativo se consideran inválidas:
        // determinista, sin azar (el orquestador debe ser reproducible).
        if (factura.Importe <= 0)
        {
            logger.LogWarning("Factura {Id} inválida (importe {Importe})",
                factura.Id, factura.Importe);
            return new ResultadoFactura(factura.Id, false, 0, "Importe no positivo");
        }

        logger.LogInformation("Factura {Id} procesada ({Importe})",
            factura.Id, factura.Importe);
        return new ResultadoFactura(factura.Id, true, factura.Importe, null);
    }
}
