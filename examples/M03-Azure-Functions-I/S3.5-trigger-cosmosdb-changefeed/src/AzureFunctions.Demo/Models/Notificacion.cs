namespace AzureFunctions.Demo.Models;

// Slide 8 — registro de la notificación que se enviaría al cliente.
// La clave de idempotencia es (PedidoId, Estado): el Change Feed puede
// entregar el mismo cambio dos veces (slide 10 — at-least-once delivery).
public sealed record Notificacion(
    string PedidoId,
    string ClienteId,
    string Estado,
    string Mensaje,
    DateTimeOffset EnviadaEn);
