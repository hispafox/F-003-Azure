using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// La "estrategia completa de errores" (slide 13) materializada:
//
//   1. Idempotencia (slide 10): si el id ya se procesó → Complete (skip).
//   2. JSON malformado → Permanente → DeadLetter inmediato (no malgastar
//      los reintentos del trigger en algo que nunca va a parsear).
//   3. El trabajo real va por IResilientApiClient (Polly: retry + circuit
//      breaker, slide 9).
//   4. Si lanza, IErrorClassifier decide:
//        Transitorio  → Abandon (SB reintenta con su maxDeliveryCount)
//        Permanente   → DeadLetter (con motivo y descripción)
//        Desconocido  → log CRITICAL + Abandon (puede ser transitorio oculto)
//
// NOTA sobre [ExponentialBackoffRetry] (slide 5): los atributos de retry
// a nivel de función NO son válidos sobre un ServiceBusTrigger en el
// isolated worker (el analyzer AZFW0012 lo rechaza en compilación).
// Service Bus YA trae su propio mecanismo: maxDeliveryCount de la cola
// (slide 4) + nuestro Abandon explícito. El [ExponentialBackoffRetry]
// solo aplica a triggers SIN retry propio (Timer, Event Hub). Lo
// documentamos aquí y en el README en vez de romper el build.
public sealed class ProcesarPedidoFunction
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IErrorClassifier _classifier;
    private readonly IIdempotencyStore _idempotencia;
    private readonly IResilientApiClient _api;
    private readonly IEstadoTracker _tracker;
    private readonly ILogger<ProcesarPedidoFunction> _logger;

    public ProcesarPedidoFunction(
        IErrorClassifier classifier,
        IIdempotencyStore idempotencia,
        IResilientApiClient api,
        IEstadoTracker tracker,
        ILogger<ProcesarPedidoFunction> logger)
    {
        _classifier = classifier;
        _idempotencia = idempotencia;
        _api = api;
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(ProcesarPedidoCola))]
    public Task ProcesarPedidoCola(
        [ServiceBusTrigger("pedidos-procesar", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
        => ProcesarAsync(mensaje, actions);

    internal async Task<ResultadoProcesamiento> ProcesarAsync(
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
    {
        var raw = mensaje.Body.ToString();

        // ── (2) Parseo: fallo permanente → DLQ inmediato ──
        Pedido? pedido;
        try
        {
            pedido = JsonSerializer.Deserialize<Pedido>(raw, JsonOpts);
        }
        catch (JsonException ex)
        {
            // Slide 11 — logging estructurado con scope (correlation id).
            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["MessageId"] = mensaje.MessageId ?? "?",
                ["DeliveryCount"] = mensaje.DeliveryCount,
            });
            _logger.LogError(ex, "Mensaje malformado → dead-letter (permanente)");
            await actions.DeadLetterMessageAsync(
                mensaje, propertiesToModify: null,
                deadLetterReason: "MalformedJson",
                deadLetterErrorDescription: ex.Message);
            _tracker.EnviadoADeadLetter(mensaje.MessageId ?? "?", "MalformedJson");
            return new ResultadoProcesamiento("DeadLetter", "MalformedJson");
        }

        if (pedido is null || string.IsNullOrEmpty(pedido.Id))
        {
            await actions.DeadLetterMessageAsync(
                mensaje, propertiesToModify: null,
                deadLetterReason: "BusinessRule",
                deadLetterErrorDescription: "Falta el id del pedido");
            _tracker.EnviadoADeadLetter(mensaje.MessageId ?? "?", "BusinessRule");
            return new ResultadoProcesamiento("DeadLetter", "BusinessRule");
        }

        // ── (1) Idempotencia: ¿ya lo procesamos? ──
        if (_idempotencia.YaProcesado(pedido.Id))
        {
            _logger.LogInformation(
                "Pedido {Id} ya procesado (duplicado SB at-least-once) → skip", pedido.Id);
            _tracker.DuplicadoSaltado(pedido.Id);
            await actions.CompleteMessageAsync(mensaje);
            return new ResultadoProcesamiento("Complete", "Duplicado");
        }

        // ── (3-4) Trabajo real con resiliencia + clasificación de errores ──
        try
        {
            await _api.EjecutarAsync(async _ =>
            {
                // Aquí iría la llamada externa real (pago, email...).
                // Simulado: éxito. Las excepciones de prueba se inyectan
                // desde los tests sustituyendo IResilientApiClient.
                await Task.CompletedTask;
                return pedido.Id;
            });

            // Registrar DESPUÉS del trabajo: si el trabajo falla no marcamos
            // como procesado (así un reintento sí lo reprocesa).
            _idempotencia.TryRegistrar(pedido.Id);
            _tracker.Procesado(pedido.Id);
            await actions.CompleteMessageAsync(mensaje);
            return new ResultadoProcesamiento("Complete", pedido.Id);
        }
        catch (Exception ex)
        {
            var tipo = _classifier.Clasificar(ex);
            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["PedidoId"] = pedido.Id,
                ["DeliveryCount"] = mensaje.DeliveryCount,
                ["TipoError"] = tipo.ToString(),
            });

            switch (tipo)
            {
                case TipoError.Transitorio:
                    _logger.LogWarning(ex, "Error transitorio → abandon (reintento)");
                    await actions.AbandonMessageAsync(mensaje);
                    return new ResultadoProcesamiento("Abandon", tipo.ToString());

                case TipoError.Permanente:
                    _logger.LogError(ex, "Error permanente → dead-letter");
                    await actions.DeadLetterMessageAsync(
                        mensaje, propertiesToModify: null,
                        deadLetterReason: "BusinessRule",
                        deadLetterErrorDescription: ex.Message);
                    _tracker.EnviadoADeadLetter(pedido.Id, "Permanente");
                    return new ResultadoProcesamiento("DeadLetter", tipo.ToString());

                default: // Desconocido
                    _logger.LogCritical(ex, "Error DESCONOCIDO → abandon + investigar");
                    await actions.AbandonMessageAsync(mensaje);
                    return new ResultadoProcesamiento("Abandon", tipo.ToString());
            }
        }
    }
}

public sealed record ResultadoProcesamiento(string Accion, string Detalle);
