using System.Text.Json.Serialization;

namespace AzureFunctions.Demo.Models;

// Documento que vive en Cosmos DB, contenedor "pedidos".
// Cosmos serializa con lowerCamelCase por defecto (slide 4 — _ts es el
// timestamp interno). Los outputs bindings respetan la misma convención.
public class Pedido
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("clienteId")]
    public string ClienteId { get; set; } = "";

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "nuevo";

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("notas")]
    public string? Notas { get; set; }

    [JsonPropertyName("creadoEn")]
    public DateTimeOffset CreadoEn { get; set; }

    [JsonPropertyName("_ts")]
    public long Timestamp { get; set; }
}
