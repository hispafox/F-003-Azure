using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 1/3 de Service Bus — Queue consumer con peek-lock (slide 18).
//
// Recibimos ServiceBusReceivedMessage (no string) para tener acceso a:
//   - MessageId (deduplicación, slide 15)
//   - DeliveryCount (cuántos reintentos llevamos)
//   - ApplicationProperties (metadata custom)
//
// Acciones explícitas mediante ServiceBusMessageActions:
//   - Complete: éxito → mensaje borrado de la cola
//   - Abandon: error transitorio → vuelve a la cola
//   - DeadLetter: error permanente → va al DLQ
public sealed class ProcesarPedidoFunction
{
    private readonly IEstadoTracker _tracker;
    private readonly ILogger<ProcesarPedidoFunction> _logger;

    public ProcesarPedidoFunction(IEstadoTracker tracker, ILogger<ProcesarPedidoFunction> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(ProcesarPedidoCola))]
    public Task ProcesarPedidoCola(
        [ServiceBusTrigger("pedidos-procesar", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
    {
        return ProcesarAsync(mensaje, actions);
    }

    // Handler puro: recibe el cuerpo deserializado y los counters/actions.
    // Devuelve el resultado para tests. Slide 26 estrategia 1.
    internal async Task<ResultadoProcesamiento> ProcesarAsync(
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
    {
        var raw = mensaje.Body.ToString();
        Pedido? pedido;
        try
        {
            pedido = JsonSerializer.Deserialize<Pedido>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException ex)
        {
            // Slide 18 — error permanente: payload corrupto. Va a DLQ y no
            // se reintenta. Sin esto el mensaje volvería a procesarse hasta
            // agotar maxDeliveryCount, gastando recursos en cada intento.
            _logger.LogError(ex, "Mensaje malformado: {Raw}", raw);
            await actions.DeadLetterMessageAsync(
                mensaje,
                deadLetterReason: "MalformedJson",
                deadLetterErrorDescription: ex.Message);
            _tracker.Abandonado(mensaje.MessageId ?? "?", "MalformedJson");
            return new ResultadoProcesamiento("DeadLetter", "MalformedJson");
        }

        if (pedido is null || string.IsNullOrEmpty(pedido.Id))
        {
            await actions.DeadLetterMessageAsync(
                mensaje,
                deadLetterReason: "EmptyPedido",
                deadLetterErrorDescription: "Falta el id del pedido");
            _tracker.Abandonado(mensaje.MessageId ?? "?", "EmptyPedido");
            return new ResultadoProcesamiento("DeadLetter", "EmptyPedido");
        }

        try
        {
            // Aquí iría el trabajo real (enviar email, generar factura...).
            // Para la demo, solo logueamos y contamos.
            _logger.LogInformation(
                "Procesando pedido {Id} (deliveryCount={DC}) cliente={Cliente} total={Total}",
                pedido.Id, mensaje.DeliveryCount, pedido.ClienteEmail, pedido.Total);

            _tracker.ProcesadoCola(pedido.Id);

            await actions.CompleteMessageAsync(mensaje);
            return new ResultadoProcesamiento("Complete", pedido.Id);
        }
        catch (Exception ex)
        {
            // Slide 18 — error temporal: abandon → vuelve a la cola para
            // reintento. Si pasa maxDeliveryCount, va al DLQ automáticamente.
            _logger.LogWarning(ex, "Error transitorio procesando {Id}, abandon", pedido.Id);
            await actions.AbandonMessageAsync(mensaje);
            return new ResultadoProcesamiento("Abandon", pedido.Id);
        }
    }
}

public sealed record ResultadoProcesamiento(string Accion, string Detalle);
