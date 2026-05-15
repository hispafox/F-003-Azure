using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 5/6 — Activities: AQUÍ sí se hace I/O y lógica. Son adaptadores
// finos sobre los servicios inyectados. El orquestador NUNCA toca esto
// directamente, solo via context.CallActivityAsync.
public sealed class PedidoActivities
{
    private readonly IPedidoValidador _validador;
    private readonly IInventarioService _inventario;
    private readonly IPagoService _pago;
    private readonly INotificacionService _notificacion;
    private readonly ILogger<PedidoActivities> _logger;

    public PedidoActivities(
        IPedidoValidador validador,
        IInventarioService inventario,
        IPagoService pago,
        INotificacionService notificacion,
        ILogger<PedidoActivities> logger)
    {
        _validador = validador;
        _inventario = inventario;
        _pago = pago;
        _notificacion = notificacion;
        _logger = logger;
    }

    [Function(nameof(ValidarPedido))]
    public Pedido ValidarPedido([ActivityTrigger] Pedido pedido)
    {
        _validador.Validar(pedido);
        return pedido;
    }

    [Function(nameof(ReservarInventario))]
    public Reserva ReservarInventario([ActivityTrigger] Pedido pedido)
        => _inventario.Reservar(pedido);

    [Function(nameof(ProcesarPago))]
    public Pago ProcesarPago([ActivityTrigger] PagoInput input)
        => _pago.Cobrar(input.Pedido, input.Reserva);

    [Function(nameof(EnviarConfirmacion))]
    public void EnviarConfirmacion([ActivityTrigger] ConfirmacionInput input)
        => _notificacion.EnviarConfirmacion(input.Pedido, input.Pago);

    [Function(nameof(NotificarManager))]
    public void NotificarManager([ActivityTrigger] Pedido pedido)
        => _notificacion.NotificarManager(pedido);

    [Function(nameof(NotificarRechazo))]
    public void NotificarRechazo([ActivityTrigger] RechazoInput input)
        => _notificacion.NotificarRechazo(input.Pedido, input.Motivo);

    // Compensación de la saga (slide 13): liberar la reserva si el pago
    // falla tras agotar reintentos. Idempotente: liberar dos veces no rompe.
    [Function(nameof(CompensarPedido))]
    public void CompensarPedido([ActivityTrigger] string reservaId)
    {
        _logger.LogWarning("Compensando: liberando reserva {ReservaId}", reservaId);
        _inventario.Liberar(reservaId);
    }
}

// Inputs de activities que necesitan más de un parámetro: Durable serializa
// el input a JSON, así que usamos records (no tipos anónimos ni dynamic).
public sealed record PagoInput(Pedido Pedido, Reserva Reserva);
public sealed record ConfirmacionInput(Pedido Pedido, Pago Pago);
public sealed record RechazoInput(Pedido Pedido, string Motivo);
