using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 16 — cuando un mensaje ya está en la DLQ, decidir qué hacer con él
// según el motivo y la descripción que Service Bus puso al dead-letterearlo.
public interface IPoisonClassifier
{
    PoisonAction Clasificar(string? deadLetterReason, string? deadLetterDescription);
}

public sealed class PoisonClassifier : IPoisonClassifier
{
    public PoisonAction Clasificar(string? deadLetterReason, string? deadLetterDescription)
    {
        var reason = deadLetterReason ?? "";
        var desc = deadLetterDescription ?? "";

        // JSON malformado: nunca se va a poder parsear → descartar (con log).
        if (desc.Contains("Json", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("Malformed", StringComparison.OrdinalIgnoreCase))
            return PoisonAction.Discard;

        // Timeout: pudo ser un pico transitorio → reintentar con delay + aviso.
        if (desc.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return PoisonAction.NotifyAndRetry;

        // Agotó los reintentos del trigger: algo va mal de forma persistente
        // → cuarentena para revisión manual.
        if (reason.Contains("MaxDeliveryCount", StringComparison.OrdinalIgnoreCase))
            return PoisonAction.Quarantine;

        // Error de negocio explícito: reintentar no ayuda → cuarentena.
        if (reason.Contains("BusinessRule", StringComparison.OrdinalIgnoreCase))
            return PoisonAction.Quarantine;

        // Desconocido: por seguridad, cuarentena (no descartar a ciegas).
        return PoisonAction.Quarantine;
    }
}
