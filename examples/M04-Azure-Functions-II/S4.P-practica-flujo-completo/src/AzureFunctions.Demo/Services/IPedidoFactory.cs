using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Función 1 (HTTP) — construir el Pedido desde el DTO y calcular el total.
// Lógica pura → testeable sin Cosmos.
public interface IPedidoFactory
{
    (IReadOnlyList<string> errores, Pedido? pedido) Crear(CrearPedidoDto? dto);
}

public sealed class PedidoFactory : IPedidoFactory
{
    public (IReadOnlyList<string> errores, Pedido? pedido) Crear(CrearPedidoDto? dto)
    {
        var errores = new List<string>();
        if (dto is null) { errores.Add("Body obligatorio"); return (errores, null); }
        if (string.IsNullOrWhiteSpace(dto.ClienteId)) errores.Add("ClienteId obligatorio");
        if (dto.Items is null || dto.Items.Count == 0) errores.Add("El pedido necesita items");
        else if (dto.Items.Any(i => i.Cantidad <= 0 || i.PrecioUnitario < 0))
            errores.Add("Items con cantidad>0 y precio>=0");

        if (errores.Count > 0) return (errores, null);

        var items = dto!.Items!.Select(i => new ItemPedido
        {
            ProductoId = i.ProductoId,
            Nombre = i.Nombre,
            Cantidad = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario,
        }).ToList();

        var pedido = new Pedido
        {
            ClienteId = dto.ClienteId,
            ClienteNombre = dto.ClienteNombre,
            Items = items,
            Total = items.Sum(i => i.Cantidad * i.PrecioUnitario),
            Estado = "nuevo",
        };
        return (Array.Empty<string>(), pedido);
    }
}
