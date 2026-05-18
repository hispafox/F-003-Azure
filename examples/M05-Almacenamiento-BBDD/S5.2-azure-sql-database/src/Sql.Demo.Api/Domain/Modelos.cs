namespace Sql.Demo.Api.Domain;

// Slide 7 — entidades relacionales. Producto 1—N Pedido (FK + navegación).
public sealed class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public ICollection<Pedido> Pedidos { get; set; } = [];
}

public sealed class Pedido
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal Total { get; set; }          // Precio × Cantidad al crear
    // DateTime (UTC), no DateTimeOffset: SQLite no soporta ORDER BY sobre
    // DateTimeOffset y la query lo ordena (gotcha EF Core documentado).
    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }      // navegación (Include, slide 31)
}

// DTOs de entrada/salida — la API nunca expone las entidades EF directas.
public sealed record CrearProductoDto(string Nombre, decimal Precio, int Stock);
public sealed record ActualizarProductoDto(decimal Precio, int Stock);
public sealed record CrearPedidoDto(int ProductoId, int Cantidad);

public sealed record PedidoDto(
    int Id, int ProductoId, string ProductoNombre,
    int Cantidad, decimal Total, DateTime Fecha);

// Resultado de negocio de crear un pedido (sin excepciones para el flujo
// esperado "no hay stock"): lo testeamos con SQLite in-memory.
public enum CrearPedidoResultado { Ok, ProductoNoExiste, StockInsuficiente }
