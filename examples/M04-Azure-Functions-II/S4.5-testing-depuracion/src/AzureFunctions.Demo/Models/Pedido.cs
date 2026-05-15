namespace AzureFunctions.Demo.Models;

public sealed record Pedido(string Id, string ClienteId, decimal Total);

public sealed record PedidoConDescuento(
    string Id,
    decimal Total,
    decimal Descuento,
    decimal TotalFinal);

// Slide 11 — resultado del procesado del CSV (lógica extraída del blob trigger).
public sealed record ResumenCsv(
    string Archivo,
    int TotalFilas,
    IReadOnlyList<string> Columnas);
