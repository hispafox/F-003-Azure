using System.Collections.Concurrent;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryEstadoTracker : IEstadoTracker
{
    private int _encolados, _procesados, _notificaciones, _clasificados, _abandonados;
    private readonly ConcurrentQueue<EntradaEstado> _entradas = new();
    private const int MaxEntradas = 50;

    public void Encolado(string pedidoId)
    {
        Interlocked.Increment(ref _encolados);
        Log("Encolado", pedidoId);
    }

    public void ProcesadoCola(string pedidoId)
    {
        Interlocked.Increment(ref _procesados);
        Log("ProcesadoCola", pedidoId);
    }

    public void NotificadoPorTopic(string pedidoId, string subscripcion)
    {
        Interlocked.Increment(ref _notificaciones);
        Log($"Notificado:{subscripcion}", pedidoId);
    }

    public void ClasificadoArchivo(string url, string clasificacion)
    {
        Interlocked.Increment(ref _clasificados);
        Log($"Clasificado:{clasificacion}", url);
    }

    public void Abandonado(string pedidoId, string motivo)
    {
        Interlocked.Increment(ref _abandonados);
        Log($"Abandonado:{motivo}", pedidoId);
    }

    public EstadoSnapshot Snapshot()
    {
        var ultimas = _entradas
            .OrderByDescending(e => e.En)
            .Take(10)
            .ToList();
        return new EstadoSnapshot(
            Encolados: Volatile.Read(ref _encolados),
            Procesados: Volatile.Read(ref _procesados),
            Notificaciones: Volatile.Read(ref _notificaciones),
            Clasificados: Volatile.Read(ref _clasificados),
            Abandonados: Volatile.Read(ref _abandonados),
            UltimasEntradas: ultimas);
    }

    private void Log(string tipo, string detalle)
    {
        _entradas.Enqueue(new EntradaEstado(tipo, detalle, DateTimeOffset.UtcNow));
        // Mantén la cola acotada (best-effort: no garantiza el límite exacto bajo
        // concurrencia, pero evita crecer sin fin en pruebas largas).
        while (_entradas.Count > MaxEntradas && _entradas.TryDequeue(out _)) { }
    }
}
