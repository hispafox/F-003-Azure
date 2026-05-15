using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Seed con 3 productos para tener algo que listar en el primer GET.
public sealed class InMemoryProductoService : IProductoService
{
    private readonly ConcurrentDictionary<string, Producto> _store;

    public InMemoryProductoService()
    {
        _store = new ConcurrentDictionary<string, Producto>(
            new[]
            {
                new KeyValuePair<string, Producto>("1", new Producto("1", "Laptop", 999.99m)),
                new KeyValuePair<string, Producto>("2", new Producto("2", "Monitor", 349.99m)),
                new KeyValuePair<string, Producto>("3", new Producto("3", "Teclado", 79.99m)),
            });
    }

    public IReadOnlyList<Producto> Listar() => _store.Values.OrderBy(p => p.Id).ToList();

    public Producto? GetById(string id) => _store.TryGetValue(id, out var p) ? p : null;

    public Producto Crear(CrearProductoDto dto)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var producto = new Producto(id, dto.Nombre, dto.Precio);
        _store[id] = producto;
        return producto;
    }

    public int Total => _store.Count;
}
