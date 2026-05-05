using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Registrado como Singleton: el estado persiste entre ejecuciones del timer
// mientras la instancia esté caliente. En isolated worker 2.x NO existe el
// atributo [Singleton] que el material lectivo de la slide 8 muestra para
// in-process; en su lugar conseguimos idempotencia con GetOrAdd.
public sealed class InMemoryInformeService(IProductoService productos) : IInformeService
{
    private readonly ConcurrentDictionary<string, Informe> _store = new();

    public (bool yaExistia, Informe informe) GenerarSiNoExiste(DateOnly fecha)
    {
        var id = $"informe-{fecha:yyyy-MM-dd}";

        // Fast path: ya existe.
        if (_store.TryGetValue(id, out var existente))
        {
            return (yaExistia: true, informe: existente);
        }

        // No existe: lo construimos y probamos a insertar. TryAdd devuelve
        // true UNA SOLA vez bajo contención. (GetOrAdd con factory puede
        // invocar el factory varias veces aunque solo un valor "gane", con
        // lo que el flag "yaExistia=false" se ponía en varios callers.)
        var stats = productos.GetStats();
        var nuevo = new Informe(
            Id: id,
            Fecha: fecha,
            TotalProductos: stats.Total,
            ProductosSinStock: stats.SinStock,
            ValorTotalStock: stats.ValorTotalStock,
            GeneradoEn: DateTimeOffset.UtcNow);

        if (_store.TryAdd(id, nuevo))
        {
            return (yaExistia: false, informe: nuevo);
        }

        // Otro hilo nos ganó la carrera entre el TryGetValue y el TryAdd.
        return (yaExistia: true, informe: _store[id]);
    }

    public Informe? GetByFecha(DateOnly fecha) =>
        _store.TryGetValue($"informe-{fecha:yyyy-MM-dd}", out var i) ? i : null;

    public IReadOnlyList<Informe> Listar() =>
        _store.Values.OrderByDescending(i => i.Fecha).ToList();
}
