namespace AzureFunctions.Demo.Models;

// El mensaje que viaja por la cola "pedidos-procesar".
public sealed record Pedido(
    string Id,
    string ClienteId,
    string ClienteEmail,
    decimal Total);

// Slide 3 — clasificación de errores que decide la estrategia de manejo.
public enum TipoError
{
    Transitorio,   // timeout, 429, 503 → reintentar con backoff
    Permanente,    // datos inválidos, 404, business rule → dead-letter inmediato
    Desconocido,   // log critical + reintentar (puede ser transitorio oculto)
}

// Slide 16 — qué hacer con un mensaje que ya está en la dead-letter queue.
public enum PoisonAction
{
    Retry,          // era transitorio: reencolar a la cola original
    Quarantine,     // permanente: guardar para revisión manual + alerta
    Discard,        // basura conocida (formato viejo): descartar con log
    NotifyAndRetry, // reintentar con delay + alerta al equipo
}
