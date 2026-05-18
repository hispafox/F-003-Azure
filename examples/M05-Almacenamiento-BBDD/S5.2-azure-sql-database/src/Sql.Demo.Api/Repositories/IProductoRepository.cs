using Microsoft.EntityFrameworkCore;
using Sql.Demo.Api.Data;
using Sql.Demo.Api.Domain;

namespace Sql.Demo.Api.Repositories;

// Slide 7 — CRUD con EF Core encapsulado tras una interfaz: endpoints
// finos, lógica testeable contra SQLite in-memory.
public interface IProductoRepository
{
    Task<IReadOnlyList<Producto>> ListarAsync();
    Task<Producto?> GetAsync(int id);
    Task<Producto> CrearAsync(CrearProductoDto dto);
    Task<Producto?> ActualizarAsync(int id, ActualizarProductoDto dto);
    Task<bool> BorrarAsync(int id);
}

public sealed class ProductoRepository(VentasDbContext db) : IProductoRepository
{
    // Slide 31 (anti-pattern 2/3): lectura sin tracking y ordenada por
    // un campo indexado (slide 9). No SELECT * implícito innecesario.
    public async Task<IReadOnlyList<Producto>> ListarAsync()
        => await db.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();

    public async Task<Producto?> GetAsync(int id)
        => await db.Productos.FindAsync(id);

    public async Task<Producto> CrearAsync(CrearProductoDto dto)
    {
        var p = new Producto { Nombre = dto.Nombre, Precio = dto.Precio, Stock = dto.Stock };
        db.Productos.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public async Task<Producto?> ActualizarAsync(int id, ActualizarProductoDto dto)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return null;
        p.Precio = dto.Precio;
        p.Stock = dto.Stock;
        await db.SaveChangesAsync();
        return p;
    }

    public async Task<bool> BorrarAsync(int id)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return false;
        db.Productos.Remove(p);
        await db.SaveChangesAsync();
        return true;
    }
}
