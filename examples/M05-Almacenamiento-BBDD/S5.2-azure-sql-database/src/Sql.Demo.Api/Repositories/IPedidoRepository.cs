using Microsoft.EntityFrameworkCore;
using Sql.Demo.Api.Data;
using Sql.Demo.Api.Domain;

namespace Sql.Demo.Api.Repositories;

// Crear pedido tiene reglas de negocio reales (existe el producto, hay
// stock, descontar stock, calcular total) → es lo que más valor tiene
// testear con SQLite in-memory sin tocar Azure (slide 7).
public interface IPedidoRepository
{
    Task<(CrearPedidoResultado resultado, PedidoDto? pedido)> CrearAsync(CrearPedidoDto dto);
    Task<IReadOnlyList<PedidoDto>> ListarAsync();
    Task<PedidoDto?> GetAsync(int id);
}

public sealed class PedidoRepository(VentasDbContext db) : IPedidoRepository
{
    public async Task<(CrearPedidoResultado, PedidoDto?)> CrearAsync(CrearPedidoDto dto)
    {
        var producto = await db.Productos.FindAsync(dto.ProductoId);
        if (producto is null)
            return (CrearPedidoResultado.ProductoNoExiste, null);
        if (dto.Cantidad <= 0 || producto.Stock < dto.Cantidad)
            return (CrearPedidoResultado.StockInsuficiente, null);

        var pedido = new Pedido
        {
            ProductoId = producto.Id,
            Cantidad = dto.Cantidad,
            Total = producto.Precio * dto.Cantidad,
            Fecha = DateTime.UtcNow,
        };
        producto.Stock -= dto.Cantidad;

        // Pedido + decremento de stock en un único SaveChanges = una
        // transacción ACID (slide 12: las transacciones son la razón de
        // usar SQL aquí).
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        return (CrearPedidoResultado.Ok, ToDto(pedido, producto));
    }

    // Slide 31 (anti-pattern 3): Include explícito + proyección a DTO en
    // una sola query — evita el N+1 de tocar pedido.Producto en un bucle.
    public async Task<IReadOnlyList<PedidoDto>> ListarAsync()
        => await db.Pedidos
            .AsNoTracking()
            .Include(p => p.Producto)
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PedidoDto(
                p.Id, p.ProductoId, p.Producto!.Nombre,
                p.Cantidad, p.Total, p.Fecha))
            .ToListAsync();

    public async Task<PedidoDto?> GetAsync(int id)
        => await db.Pedidos
            .AsNoTracking()
            .Include(p => p.Producto)
            .Where(p => p.Id == id)
            .Select(p => new PedidoDto(
                p.Id, p.ProductoId, p.Producto!.Nombre,
                p.Cantidad, p.Total, p.Fecha))
            .FirstOrDefaultAsync();

    private static PedidoDto ToDto(Pedido p, Producto pr)
        => new(p.Id, p.ProductoId, pr.Nombre, p.Cantidad, p.Total, p.Fecha);
}
