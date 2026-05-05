using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Backing store en memoria thread-safe. Simula un repositorio para que el
// ejemplo no dependa de Cosmos/SQL todavía (eso llega en S3.5/S3.6).
// Slide 25 (S3.1) — connection pooling: este servicio se registra como
// SINGLETON en Program.cs para que no se recree por ejecución.
public sealed class InMemoryProductoService : IProductoService
{
    private readonly ConcurrentDictionary<string, Producto> _store = new();

    public InMemoryProductoService()
    {
        // Datos de demo — útiles tras un deploy para que GET /api/productos
        // devuelva algo aunque el alumno no haya hecho un POST todavía.
        Seed("p-001", "Laptop Pro", "electronica", 1299.99m, 8);
        Seed("p-002", "Auriculares BT", "electronica", 79.50m, 32);
        Seed("p-003", "Camiseta Curso AZ-204", "ropa", 19.90m, 100);
    }

    private void Seed(string id, string nombre, string categoria, decimal precio, int stock) =>
        _store.TryAdd(id, new Producto
        {
            Id = id,
            Nombre = nombre,
            Categoria = categoria,
            Precio = precio,
            Stock = stock
        });

    public IReadOnlyList<Producto> Buscar(BuscarProductosQuery query, out int totalSinPaginar)
    {
        IEnumerable<Producto> filtrados = _store.Values;

        if (!string.IsNullOrWhiteSpace(query.Nombre))
        {
            filtrados = filtrados.Where(p =>
                p.Nombre.Contains(query.Nombre, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Categoria))
        {
            filtrados = filtrados.Where(p =>
                string.Equals(p.Categoria, query.Categoria, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinPrecio.HasValue)
            filtrados = filtrados.Where(p => p.Precio >= query.MinPrecio.Value);

        if (query.MaxPrecio.HasValue)
            filtrados = filtrados.Where(p => p.Precio <= query.MaxPrecio.Value);

        var ordenados = filtrados.OrderBy(p => p.Id).ToList();
        totalSinPaginar = ordenados.Count;

        return ordenados
            .Skip((query.Pagina - 1) * query.PorPagina)
            .Take(query.PorPagina)
            .ToList();
    }

    public Producto? GetById(string id) =>
        _store.TryGetValue(id, out var producto) ? producto : null;

    public Producto Crear(CrearProductoDto dto)
    {
        var producto = new Producto
        {
            Id = $"p-{Guid.NewGuid().ToString("N")[..8]}",
            Nombre = dto.Nombre,
            Categoria = dto.Categoria,
            Precio = dto.Precio,
            Stock = dto.Stock
        };
        _store[producto.Id] = producto;
        return producto;
    }

    public Producto? Actualizar(string id, ActualizarProductoDto dto)
    {
        if (!_store.TryGetValue(id, out var actual)) return null;

        if (dto.Nombre is not null) actual.Nombre = dto.Nombre;
        if (dto.Categoria is not null) actual.Categoria = dto.Categoria;
        if (dto.Precio.HasValue) actual.Precio = dto.Precio.Value;
        if (dto.Stock.HasValue) actual.Stock = dto.Stock.Value;
        actual.ActualizadoEn = DateTimeOffset.UtcNow;

        return actual;
    }

    public bool Eliminar(string id) => _store.TryRemove(id, out _);
}
