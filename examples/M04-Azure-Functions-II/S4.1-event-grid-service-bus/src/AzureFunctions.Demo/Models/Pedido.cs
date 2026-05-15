namespace AzureFunctions.Demo.Models;

// Slide 23 — Pedido del sistema async. El HTTP encola; el SB trigger lo
// procesa; el SB topic notifica a multiples suscriptores en paralelo.
public sealed record Pedido(
    string Id,
    string ClienteId,
    string ClienteEmail,
    decimal Total,
    string? Notas,
    DateTimeOffset CreadoEn);

public sealed record CrearPedidoDto(
    string ClienteId,
    string ClienteEmail,
    decimal Total,
    string? Notas);
