namespace Cosmos.Demo.Api.Domain;

// Slide 14, 22, 31 — modelo DESNORMALIZADO (embed): el pedido lleva el
// nombre del cliente y los items dentro, para resolver "pedido completo"
// con 1 lectura / 1 RU. PartitionKey = /clienteId (slide 6).
//
// Serializa con CamelCase (Program.cs) → Id→"id" (lo exige Cosmos),
// ClienteId→"clienteId". Sin atributos de serializador (POCO).
public sealed class Pedido
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClienteId { get; set; } = "";        // partition key
    public string ClienteNombre { get; set; } = "";    // desnormalizado
    public List<PedidoItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    // Slide 12 — Change Feed no registra DELETE: se usa soft delete y la
    // función downstream reacciona al campo `eliminado`.
    public bool Eliminado { get; set; }
    public DateTimeOffset? EliminadoEn { get; set; }
}

public sealed record PedidoItem(string ProductoId, string Nombre, int Cantidad, decimal Precio);

// Slide 17 — TransactionalBatch: pedido + movimiento de saldo en la
// MISMA partición lógica (/clienteId), atómico.
public sealed class Movimiento
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClienteId { get; set; } = "";
    public string Tipo { get; set; } = "";             // "cargo" | "abono"
    public decimal Importe { get; set; }
}

// DTOs de entrada — la API no expone los documentos directamente.
public sealed record CrearPedidoItemDto(string ProductoId, string Nombre, int Cantidad, decimal Precio);
public sealed record CrearPedidoDto(string ClienteId, string ClienteNombre, List<CrearPedidoItemDto> Items);

public sealed record PedidoCreadoDto(string Id, string ClienteId, decimal Total, double RuConsumidas);
