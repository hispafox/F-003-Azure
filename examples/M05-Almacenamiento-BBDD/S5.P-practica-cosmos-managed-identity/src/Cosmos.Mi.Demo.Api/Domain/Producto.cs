namespace Cosmos.Mi.Demo.Api.Domain;

// Slide 8 — documento de la práctica. PartitionKey = /categoria (slide 4).
// camelCase (Program.cs): Id→"id" (lo exige Cosmos), Categoria→"categoria".
public sealed class Producto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Categoria { get; set; } = "";   // partition key
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
}

public sealed record CrearProductoDto(string Categoria, string Nombre, decimal Precio);

public sealed record ProductoCreadoDto(string Id, string Categoria, double RuConsumidas);
