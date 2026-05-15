using System.Collections.Concurrent;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryEstadoTracker : IEstadoTracker
{
    private int _procesados, _duplicados, _deadLetter, _poison;
    private readonly ConcurrentQueue<EntradaEstado> _entradas = new();
    private const int MaxEntradas = 50;

    public void Procesado(string pedidoId)
    {
        Interlocked.Increment(ref _procesados);
        Log("Procesado", pedidoId);
    }

    public void DuplicadoSaltado(string pedidoId)
    {
        Interlocked.Increment(ref _duplicados);
        Log("DuplicadoSaltado", pedidoId);
    }

    public void EnviadoADeadLetter(string pedidoId, string motivo)
    {
        Interlocked.Increment(ref _deadLetter);
        Log($"DeadLetter:{motivo}", pedidoId);
    }

    public void PoisonProcesado(string pedidoId, string accion)
    {
        Interlocked.Increment(ref _poison);
        Log($"Poison:{accion}", pedidoId);
    }

    public EstadoSnapshot Snapshot()
    {
        var ultimas = _entradas
            .OrderByDescending(e => e.En)
            .Take(10)
            .ToList();
        return new EstadoSnapshot(
            Procesados: Volatile.Read(ref _procesados),
            DuplicadosSaltados: Volatile.Read(ref _duplicados),
            EnviadosADeadLetter: Volatile.Read(ref _deadLetter),
            PoisonProcesados: Volatile.Read(ref _poison),
            UltimasEntradas: ultimas);
    }

    private void Log(string tipo, string detalle)
    {
        _entradas.Enqueue(new EntradaEstado(tipo, detalle, DateTimeOffset.UtcNow));
        while (_entradas.Count > MaxEntradas && _entradas.TryDequeue(out _)) { }
    }
}
