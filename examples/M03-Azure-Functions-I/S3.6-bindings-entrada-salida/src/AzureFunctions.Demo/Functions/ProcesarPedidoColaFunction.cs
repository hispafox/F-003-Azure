using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 19 — Queue trigger sobre Storage Queue. Lee el mensaje que
// CrearPedidoFunction encoló.
//
// Slide 21 (anti-pattern aware) — Leemos el cuerpo como STRING crudo
// y deserializamos a mano. Si bindáramos directamente a un POCO y el
// mensaje fuera JSON inválido, Functions reintenta 5 veces (maxDequeueCount
// default) y luego lo manda a poison queue, pero el log es genérico y
// perdemos visibilidad. Con string crudo + try/catch logueamos el
// payload exacto que falla y decidimos qué hacer.
public sealed class ProcesarPedidoColaFunction
{
    private readonly ILogger<ProcesarPedidoColaFunction> _logger;

    public ProcesarPedidoColaFunction(ILogger<ProcesarPedidoColaFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcesarPedidoCola))]
    public void ProcesarPedidoCola(
        [QueueTrigger("pedidos-pendientes", Connection = "AzureWebJobsStorage")]
        string mensajeRaw)
    {
        Procesar(mensajeRaw);
    }

    // Handler puro para tests (slide 26 estrategia 1).
    internal MensajePedidoCola? Procesar(string mensajeRaw)
    {
        if (string.IsNullOrWhiteSpace(mensajeRaw))
        {
            _logger.LogWarning("Mensaje vacío en pedidos-pendientes, descartado");
            return null;
        }

        MensajePedidoCola? mensaje;
        try
        {
            mensaje = JsonSerializer.Deserialize<MensajePedidoCola>(
                mensajeRaw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            // Loguear el payload exacto antes de relanzar/descartar.
            // En producción mandaríamos a un dead-letter dedicado.
            _logger.LogError(ex,
                "Mensaje JSON inválido en pedidos-pendientes: {Raw}", mensajeRaw);
            return null;
        }

        if (mensaje is null || string.IsNullOrEmpty(mensaje.PedidoId))
        {
            _logger.LogWarning(
                "Mensaje sin PedidoId, descartado: {Raw}", mensajeRaw);
            return null;
        }

        _logger.LogInformation(
            "Procesando mensaje de cola: pedido={PedidoId} cliente={ClienteId} total={Total}",
            mensaje.PedidoId, mensaje.ClienteId, mensaje.Total);

        // Aquí iría la lógica real: llamar a un servicio de pago, notificar,
        // marcar el pedido como procesado en Cosmos, etc.

        return mensaje;
    }
}

public sealed record MensajePedidoCola(
    string PedidoId,
    string ClienteId,
    decimal Total,
    DateTimeOffset Encolado);
