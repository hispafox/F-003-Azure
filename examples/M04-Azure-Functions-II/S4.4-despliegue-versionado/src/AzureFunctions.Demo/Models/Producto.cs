namespace AzureFunctions.Demo.Models;

// El dominio es UNO solo. Lo que cambia entre versiones de la API es el
// CONTRATO que exponemos, no el modelo interno (slide 7).
public sealed record Producto(
    string Id,
    string Nombre,
    decimal Precio,
    string Moneda,
    int Stock);

// Contrato v1: el original. NO incluye moneda ni stock (no existían).
public sealed record ProductoV1(string Id, string Nombre, decimal Precio);

// Contrato v2: breaking change — añade moneda y stock. Un cliente v1 que
// recibiera esto podría romperse, por eso es una versión nueva, no un
// cambio in-place de v1.
public sealed record ProductoV2(
    string Id,
    string Nombre,
    decimal Precio,
    string Moneda,
    int Stock);

public sealed record Pedido(string Id, string ClienteId, decimal Total);
public sealed record ResultadoProceso(string PedidoId, string ProcesadoPor, decimal Total);
