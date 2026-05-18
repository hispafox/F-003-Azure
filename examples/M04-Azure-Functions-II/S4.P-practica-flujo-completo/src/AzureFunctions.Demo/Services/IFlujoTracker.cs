using System.Collections.Concurrent;

namespace AzureFunctions.Demo.Services;

// Slide 11 — idempotencia del flujo end-to-end. El Cosmos Change Feed
// puede re-disparar el mismo pedido (at-least-once). Registramos qué
// pedidos ya se facturaron; el segundo intento se salta.
//
// Además sirve de inspección end-to-end para /api/estado: cuántos
// pedidos creados, facturados, notificados.
public interface IFlujoTracker
{
    void PedidoCreado(string pedidoId);

    // true si es la PRIMERA vez que facturamos este pedido (procesar);
    // false si ya estaba facturado (duplicado → saltar).
    bool TryMarcarFacturado(string pedidoId);

    void Notificado(string pedidoId, string facturaNumero);

    FlujoSnapshot Snapshot();
}

public sealed record FlujoSnapshot(
    int Creados, int Facturados, int Notificados,
    IReadOnlyList<string> UltimasNotificaciones);

public sealed class InMemoryFlujoTracker : IFlujoTracker
{
    private int _creados, _notificados;
    private readonly ConcurrentDictionary<string, byte> _facturados = new();
    private readonly ConcurrentQueue<string> _notifs = new();

    public void PedidoCreado(string pedidoId) => Interlocked.Increment(ref _creados);

    // TryAdd: exactamente una llamada gana bajo contención (lección S3.5).
    public bool TryMarcarFacturado(string pedidoId)
        => _facturados.TryAdd(pedidoId, 0);

    public void Notificado(string pedidoId, string facturaNumero)
    {
        Interlocked.Increment(ref _notificados);
        _notifs.Enqueue($"{pedidoId} → {facturaNumero}");
        while (_notifs.Count > 20 && _notifs.TryDequeue(out _)) { }
    }

    public FlujoSnapshot Snapshot() => new(
        Volatile.Read(ref _creados),
        _facturados.Count,
        Volatile.Read(ref _notificados),
        _notifs.Reverse().Take(10).ToList());
}
