using System.Text.Json.Serialization;

namespace AzureFunctions.Demo.Models;

// Documento que vive en Cosmos DB, contenedor "pedidos".
// Los nombres JSON son lowerCamelCase porque así lo serializa el
// Cosmos DB SDK por defecto (slide 4 — _ts es el timestamp interno).
public class Pedido
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("clienteId")]
    public string ClienteId { get; set; } = "";

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "";

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("_ts")]
    public long Timestamp { get; set; }
}
