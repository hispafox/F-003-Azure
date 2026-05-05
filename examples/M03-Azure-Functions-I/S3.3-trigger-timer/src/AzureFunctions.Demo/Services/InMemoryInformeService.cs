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
        var yaExistia = true;

        var informe = _store.GetOrAdd(id, key =>
        {
            yaExistia = false;
            var stats = productos.GetStats();
            return new Informe(
                Id: key,
                Fecha: fecha,
                TotalProductos: stats.Total,
                ProductosSinStock: stats.SinStock,
                ValorTotalStock: stats.ValorTotalStock,
                GeneradoEn: DateTimeOffset.UtcNow);
        });

        return (yaExistia, informe);
    }

    public Informe? GetByFecha(DateOnly fecha) =>
        _store.TryGetValue($"informe-{fecha:yyyy-MM-dd}", out var i) ? i : null;

    public IReadOnlyList<Informe> Listar() =>
        _store.Values.OrderByDescending(i => i.Fecha).ToList();
}
