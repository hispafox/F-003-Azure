using AzureFunctions.Demo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slides 6, 9, 13 — Orquestador SAGA:
//   1) chaining:   Validar → Reservar → (aprobación?) → Pagar → Confirmar
//   2) human:      total > UMBRAL_APROBACION → espera evento "AprobacionManager"
//                  con timeout 72h
//   3) saga:       si Pagar falla tras reintentos → CompensarPedido (libera
//                  la reserva) y la orquestación termina como "compensado"
//
// REGLA DE ORO (slide 5): el orquestador es DETERMINISTA. Nada de
// DateTime.UtcNow, Random, I/O. Solo context.* y CallActivityAsync.
public sealed class ProcesarPedidoOrchestrator
{
    public const decimal UmbralAprobacion = 5000m;
    public const string EventoAprobacion = "AprobacionManager";

    private static readonly TaskOptions RetryActivities = new(
        new TaskRetryOptions(new RetryPolicy(
            maxNumberOfAttempts: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0)));   // 5s, 10s, 20s

    [Function(nameof(ProcesarPedido))]
    public async Task<ResultadoPedido> ProcesarPedido(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var pedido = context.GetInput<Pedido>()!;
        var logger = context.CreateReplaySafeLogger<ProcesarPedidoOrchestrator>();

        // ── Paso 1: validar (con reintentos) ──
        pedido = await context.CallActivityAsync<Pedido>(
            nameof(PedidoActivities.ValidarPedido), pedido, RetryActivities);

        // ── Paso 2: reservar inventario (con reintentos) ──
        var reserva = await context.CallActivityAsync<Reserva>(
            nameof(PedidoActivities.ReservarInventario), pedido, RetryActivities);

        // ── Paso 3 (condicional): aprobación humana si supera el umbral ──
        if (pedido.Total > UmbralAprobacion)
        {
            context.SetCustomStatus("esperando-aprobacion");
            await context.CallActivityAsync(
                nameof(PedidoActivities.NotificarManager), pedido);

            var aprobado = await EsperarAprobacionAsync(context, logger);
            if (!aprobado)
            {
                // No aprobado o expirado: compensar la reserva y terminar.
                await context.CallActivityAsync(
                    nameof(PedidoActivities.CompensarPedido), reserva.ReservaId);
                await context.CallActivityAsync(
                    nameof(PedidoActivities.NotificarRechazo),
                    new RechazoInput(pedido, "No aprobado o expirado"));
                return new ResultadoPedido(
                    pedido.Id, "rechazado", null, "No aprobado o expirado en 72h");
            }
        }

        // ── Paso 4: pagar — punto de fallo de la saga ──
        try
        {
            var pago = await context.CallActivityAsync<Pago>(
                nameof(PedidoActivities.ProcesarPago),
                new PagoInput(pedido, reserva),
                RetryActivities);

            await context.CallActivityAsync(
                nameof(PedidoActivities.EnviarConfirmacion),
                new ConfirmacionInput(pedido, pago));

            context.SetCustomStatus("completado");
            return new ResultadoPedido(pedido.Id, "completado", pago.TransaccionId, null);
        }
        catch (TaskFailedException ex)
        {
            // Slide 13 — Saga: el pago falló tras agotar reintentos.
            // Deshacemos el paso 2 (liberar inventario) y marcamos compensado.
            logger.LogError(ex, "Pago falló para {PedidoId}, compensando", pedido.Id);
            context.SetCustomStatus("compensando");

            await context.CallActivityAsync(
                nameof(PedidoActivities.CompensarPedido), reserva.ReservaId);
            await context.CallActivityAsync(
                nameof(PedidoActivities.NotificarRechazo),
                new RechazoInput(pedido, "Pago rechazado"));

            return new ResultadoPedido(
                pedido.Id, "compensado", null, "Pago rechazado tras reintentos");
        }
    }

    // Slide 9 — espera el evento externo o el timeout, lo que llegue antes.
    // Extraído a método para legibilidad; sigue siendo determinista porque
    // solo usa context.*.
    private static async Task<bool> EsperarAprobacionAsync(
        TaskOrchestrationContext context, ILogger logger)
    {
        using var cts = new CancellationTokenSource();
        var timeout = context.CreateTimer(
            context.CurrentUtcDateTime.AddHours(72), cts.Token);
        var aprobacion = context.WaitForExternalEvent<bool>(EventoAprobacion);

        var ganador = await Task.WhenAny(aprobacion, timeout);
        if (ganador == aprobacion)
        {
            cts.Cancel(); // libera el timer pendiente
            var resultado = aprobacion.Result;
            logger.LogInformation("Aprobación recibida: {Resultado}", resultado);
            return resultado;
        }

        logger.LogWarning("Aprobación expiró (72h)");
        return false;
    }
}
