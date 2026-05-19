using Azure;
using Azure.Data.Tables;

namespace Tables.Demo.Api.Domain;

// Slide 7 — entidad de Table Storage. PartitionKey = categoría,
// RowKey = id. Precio en `double`: Table Storage NO soporta `decimal`
// (slide 7) — para dinero real se usaría long en céntimos.
public sealed class Producto : ITableEntity
{
    public string PartitionKey { get; set; } = "";   // categoría
    public string RowKey { get; set; } = "";          // id del producto
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Nombre { get; set; } = "";
    public double Precio { get; set; }
    public int Stock { get; set; }
}
