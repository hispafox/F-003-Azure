using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 5 — Repositorio en memoria. ConcurrentDictionary porque la
// Function App puede atender varios requests en paralelo.
//
// Slide 6 — Registrado como Singleton: el estado vive mientras la
// instancia está caliente. Cuando la function escala o se reinicia,
// volveremos a los datos del seed (limitación deliberada de la práctica,
// slide 12 — para persistencia real → Cosmos en M05).
public sealed class InMemoryProductoService : IProductoService
{
    private readonly ConcurrentDictionary<string, Producto> _store;

    public InMemoryProductoService()
    {
        // Slide 5 — seed con 3 productos para que el primer GET no esté vacío.
        _store = new ConcurrentDictionary<string, Producto>(
            new[]
            {
                new KeyValuePair<string, Producto>("p001",
                    new Producto("p001", "Laptop Dell", 1299.00m, 5)),
                new KeyValuePair<string, Producto>("p002",
                    new Producto("p002", "Monitor 27\"", 349.00m, 12)),
                new KeyValuePair<string, Producto>("p003",
                    new Producto("p003", "Teclado mecánico", 89.90m, 30)),
            });
    }

    public IReadOnlyList<Producto> Listar() =>
        _store.Values.OrderBy(p => p.Id).ToList();

    public Producto? GetById(string id) =>
        _store.TryGetValue(id, out var p) ? p : null;

    public Producto Crear(CrearProductoDto dto)
    {
        var id = $"p{Guid.NewGuid().ToString("N")[..6]}";
        var producto = new Producto(id, dto.Nombre, dto.Precio, dto.Stock);
        _store[id] = producto;
        return producto;
    }

    public Producto? Actualizar(string id, CrearProductoDto dto)
    {
        if (!_store.ContainsKey(id)) return null;
        var producto = new Producto(id, dto.Nombre, dto.Precio, dto.Stock);
        _store[id] = producto;
        return producto;
    }

    public bool Borrar(string id) => _store.TryRemove(id, out _);

    public int Total => _store.Count;
}
