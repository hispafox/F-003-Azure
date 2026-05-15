namespace AzureFunctions.Demo.Services;

// Slide 9 — cliente hacia un servicio externo con resiliencia: retry
// exponencial + circuit breaker. La operación real se inyecta como
// delegate para poder testear el pipeline SIN HTTP de verdad.
public interface IResilientApiClient
{
    // Ejecuta la operación a través del pipeline de Polly.
    // Lanza CircuitoAbiertoException si el circuito está abierto.
    Task<T> EjecutarAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}
