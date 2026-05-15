namespace AzureFunctions.Demo.Models;

// Slide 6 — el pedido que recorre la saga: validar → reservar → pagar →
// confirmar. records inmutables; cada activity devuelve su resultado.
public sealed record Pedido(
    string Id,
    string ClienteId,
    string ClienteEmail,
    decimal Total,
    IReadOnlyList<LineaPedido> Items);

public sealed record LineaPedido(string Sku, int Cantidad, decimal PrecioUnitario);

public sealed record Reserva(string ReservaId, string PedidoId, bool Confirmada);

public sealed record Pago(string TransaccionId, string PedidoId, decimal Importe, bool Exito);

// Resultado final que el orquestador devuelve como output de la instancia.
public sealed record ResultadoPedido(
    string PedidoId,
    string Estado,        // "completado" | "rechazado" | "compensado"
    string? TransaccionId,
    string? Motivo);
