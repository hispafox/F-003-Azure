namespace AzureFunctions.Demo.Models;

// Slide 25 — record para el DTO de entrada: inmutable, con value-equality,
// y compatible con System.Text.Json sin esfuerzo adicional.
public sealed record CrearPedidoDto(
    string ClienteId,
    decimal Total,
    string? Notas);
