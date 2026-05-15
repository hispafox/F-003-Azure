using System.Collections.Concurrent;

namespace AzureFunctions.Demo.Services;

// Slide 10 — la defensa definitiva: si un mensaje se entrega dos veces
// (at-least-once de Service Bus), procesarlo dos veces NO debe duplicar
// efectos. Registramos cada id procesado; el segundo intento es noop.
//
// En producción esto sería Cosmos/Table/Redis con TTL. In-memory basta
// para el ejemplo y para los tests de concurrencia.
public interface IIdempotencyStore
{
    // true si es la PRIMERA vez que vemos esta clave (procesar);
    // false si ya estaba registrada (saltar — duplicado).
    bool TryRegistrar(string clave);
    bool YaProcesado(string clave);
    int Total { get; }
}

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, byte> _procesados = new();

    // TryAdd devuelve true exactamente una vez bajo contención (lección
    // aprendida en S3.5: GetOrAdd con factory NO sirve para esto).
    public bool TryRegistrar(string clave)
    {
        ArgumentException.ThrowIfNullOrEmpty(clave);
        return _procesados.TryAdd(clave, 0);
    }

    public bool YaProcesado(string clave) => _procesados.ContainsKey(clave);

    public int Total => _procesados.Count;
}
