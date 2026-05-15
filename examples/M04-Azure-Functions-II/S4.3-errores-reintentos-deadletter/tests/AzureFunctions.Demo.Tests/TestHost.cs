using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    // ProcesarPedido con un IResilientApiClient que ejecuta la operación
    // sin resiliencia (passthrough) salvo que el test inyecte uno propio.
    public static (ProcesarPedidoFunction fn, IEstadoTracker tracker, IIdempotencyStore idem)
        NewProcesarPedido(IResilientApiClient? api = null)
    {
        var tracker = new InMemoryEstadoTracker();
        var idem = new InMemoryIdempotencyStore();
        var fn = new ProcesarPedidoFunction(
            new ErrorClassifier(),
            idem,
            api ?? new PassthroughApiClient(),
            tracker,
            NullLogger<ProcesarPedidoFunction>.Instance);
        return (fn, tracker, idem);
    }

    public static (ProcesarDeadLetterFunction fn, IEstadoTracker tracker) NewDeadLetter()
    {
        var tracker = new InMemoryEstadoTracker();
        var fn = new ProcesarDeadLetterFunction(
            new PoisonClassifier(),
            tracker,
            NullLogger<ProcesarDeadLetterFunction>.Instance);
        return (fn, tracker);
    }
}

// Cliente "resiliente" que solo ejecuta la operación tal cual: para tests
// del flujo de la función donde la resiliencia de Polly no es lo que se
// está validando.
internal sealed class PassthroughApiClient : IResilientApiClient
{
    public Task<T> EjecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
        => operacion(ct);
}

// Cliente que siempre lanza la excepción dada (para forzar caminos de
// clasificación de errores en ProcesarPedidoFunction).
internal sealed class ThrowingApiClient(Exception ex) : IResilientApiClient
{
    public Task<T> EjecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
        => throw ex;
}
