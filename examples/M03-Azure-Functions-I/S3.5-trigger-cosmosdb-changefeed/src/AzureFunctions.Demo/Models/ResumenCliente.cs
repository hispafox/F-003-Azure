using System.Text.Json.Serialization;

namespace AzureFunctions.Demo.Models;

// Slide 9 — vista materializada que la función escribe al contenedor
// "resumenes-clientes" mediante [CosmosDBOutput]. La PK es ClienteId,
// y el Id es estable ("resumen-{clienteId}") para que sea un upsert
// natural (slide 10 — operación idempotente por construcción).
public class ResumenCliente
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("clienteId")]
    public string ClienteId { get; set; } = "";

    [JsonPropertyName("ultimoPedidoTimestamp")]
    public long UltimoPedidoTimestamp { get; set; }

    [JsonPropertyName("totalPedidos")]
    public int TotalPedidos { get; set; }

    [JsonPropertyName("importeAcumulado")]
    public decimal ImporteAcumulado { get; set; }

    [JsonPropertyName("actualizadoEn")]
    public DateTimeOffset ActualizadoEn { get; set; }
}
