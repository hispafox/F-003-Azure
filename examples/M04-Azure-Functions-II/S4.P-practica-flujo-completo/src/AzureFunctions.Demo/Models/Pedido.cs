using System.Text.Json.Serialization;

namespace AzureFunctions.Demo.Models;

// Documento en Cosmos (contenedor "pedidos"). El campo `estado` es la
// máquina de estados que da idempotencia al flujo (slide 11):
//   nuevo → facturado   (el Cosmos trigger solo procesa los "nuevo")
public sealed class Pedido
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("clienteId")]
    public string ClienteId { get; set; } = "";

    [JsonPropertyName("clienteNombre")]
    public string ClienteNombre { get; set; } = "";

    [JsonPropertyName("items")]
    public List<ItemPedido> Items { get; set; } = [];

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "nuevo";

    [JsonPropertyName("creadoEn")]
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ItemPedido
{
    [JsonPropertyName("productoId")]
    public string ProductoId { get; set; } = "";

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("precioUnitario")]
    public decimal PrecioUnitario { get; set; }
}

public sealed record CrearPedidoDto(
    string ClienteId,
    string ClienteNombre,
    List<ItemPedidoDto> Items);

public sealed record ItemPedidoDto(
    string ProductoId, string Nombre, int Cantidad, decimal PrecioUnitario);

// Lo que se serializa a Blob (factura) y a Queue (notificación).
public sealed record Factura(
    string Numero,
    string PedidoId,
    string ClienteId,
    string ClienteNombre,
    decimal Total,
    decimal Iva,
    decimal TotalConIva,
    DateTimeOffset FechaEmision);

public sealed record MensajeFactura(
    string PedidoId,
    string FacturaNumero,
    decimal TotalConIva);
