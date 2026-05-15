using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryLimpiezaTracker : ILimpiezaTracker
{
    private readonly ConcurrentQueue<LimpiezaResultado> _historial = new();

    public LimpiezaResultado Registrar(int registrosEliminados, bool llegoTarde)
    {
        var resultado = new LimpiezaResultado(
            DateTimeOffset.UtcNow, registrosEliminados, llegoTarde);
        _historial.Enqueue(resultado);
        return resultado;
    }

    public IReadOnlyList<LimpiezaResultado> Historial =>
        _historial.OrderByDescending(r => r.Ejecutado).ToList();

    public int TotalEjecuciones => _historial.Count;
}
