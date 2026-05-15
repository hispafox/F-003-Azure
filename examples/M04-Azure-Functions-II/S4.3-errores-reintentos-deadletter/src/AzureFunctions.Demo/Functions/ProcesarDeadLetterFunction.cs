using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 8/16 — el poison-message processor. Triggerea sobre la SUB-cola
// "$deadletterqueue" de la cola principal. Decide qué hacer con cada
// mensaje muerto según el motivo con el que Service Bus lo dead-leteró:
//
//   Retry          → reencolar a la cola original (era transitorio)
//   Quarantine     → guardar para revisión manual + alerta
//   Discard        → basura conocida: descartar con log
//   NotifyAndRetry → alerta + reencolar con delay
//
// En todos los casos hacemos Complete sobre el mensaje de la DLQ (sacarlo
// de ahí); si no, la DLQ crece sin fin.
public sealed class ProcesarDeadLetterFunction
{
    private readonly IPoisonClassifier _classifier;
    private readonly IEstadoTracker _tracker;
    private readonly ILogger<ProcesarDeadLetterFunction> _logger;

    public ProcesarDeadLetterFunction(
        IPoisonClassifier classifier,
        IEstadoTracker tracker,
        ILogger<ProcesarDeadLetterFunction> logger)
    {
        _classifier = classifier;
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(ProcesarDeadLetter))]
    public Task ProcesarDeadLetter(
        [ServiceBusTrigger(
            "pedidos-procesar/$deadletterqueue",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
        => ProcesarAsync(mensaje, actions);

    internal async Task<PoisonAction> ProcesarAsync(
        ServiceBusReceivedMessage mensaje,
        ServiceBusMessageActions actions)
    {
        var accion = _classifier.Clasificar(
            mensaje.DeadLetterReason, mensaje.DeadLetterErrorDescription);

        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = mensaje.MessageId ?? "?",
            ["DeadLetterReason"] = mensaje.DeadLetterReason ?? "?",
            ["Accion"] = accion.ToString(),
        });

        switch (accion)
        {
            case PoisonAction.Discard:
                _logger.LogInformation("Mensaje muerto conocido → descartado");
                break;

            case PoisonAction.Retry:
                // En producción aquí reenviarías a la cola original con un
                // ServiceBusSender. Para el ejemplo lo registramos.
                _logger.LogWarning("Reencolando a la cola original (era transitorio)");
                break;

            case PoisonAction.NotifyAndRetry:
                _logger.LogWarning("Alerta + reencolar con delay");
                break;

            case PoisonAction.Quarantine:
            default:
                _logger.LogError(
                    "Cuarentena: requiere revisión manual. Reason={Reason}",
                    mensaje.DeadLetterReason);
                break;
        }

        _tracker.PoisonProcesado(mensaje.MessageId ?? "?", accion.ToString());

        // Siempre Complete: el mensaje sale de la DLQ (ya lo hemos
        // gestionado de una u otra forma).
        await actions.CompleteMessageAsync(mensaje);
        return accion;
    }
}
