using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 10 — idempotencia por construcción: ConcurrentDictionary.GetOrAdd
// hace el check + insert en una sola operación atómica. Si el batch del
// Change Feed se reintenta (slide 10 — at-least-once), el segundo intento
// de la misma (PedidoId, Estado) NO añade nada y devuelve false.
//
// Sustituiría a una tabla "notificaciones-enviadas" en producción
// (Cosmos, Table Storage, Redis...). Para el ejemplo basta con memoria.
public sealed class InMemoryNotificacionService : INotificacionService
{
    private readonly ConcurrentDictionary<string, Notificacion> _enviadas = new();

    public bool EnviarSiNoEnviada(string pedidoId, string clienteId, string estado, string mensaje)
    {
        ArgumentException.ThrowIfNullOrEmpty(pedidoId);
        ArgumentException.ThrowIfNullOrEmpty(clienteId);
        ArgumentException.ThrowIfNullOrEmpty(estado);

        // TryAdd devuelve true exactamente una vez bajo contención.
        // GetOrAdd con factory NO sirve aquí: bajo contención el factory
        // puede invocarse varias veces aunque solo un valor "gane", lo que
        // haría que múltiples llamadores reporten "envié yo".
        var clave = Clave(pedidoId, estado);
        var notificacion = new Notificacion(
            pedidoId, clienteId, estado, mensaje, DateTimeOffset.UtcNow);

        return _enviadas.TryAdd(clave, notificacion);
    }

    public IReadOnlyCollection<Notificacion> ListarTodas()
        => _enviadas.Values.OrderBy(n => n.EnviadaEn).ToList();

    public IReadOnlyCollection<Notificacion> ListarPorCliente(string clienteId)
        => _enviadas.Values
            .Where(n => string.Equals(n.ClienteId, clienteId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.EnviadaEn)
            .ToList();

    public Notificacion? Buscar(string pedidoId, string estado)
        => _enviadas.TryGetValue(Clave(pedidoId, estado), out var n) ? n : null;

    public int Total => _enviadas.Count;

    private static string Clave(string pedidoId, string estado)
        => $"{pedidoId}::{estado}".ToLowerInvariant();
}
