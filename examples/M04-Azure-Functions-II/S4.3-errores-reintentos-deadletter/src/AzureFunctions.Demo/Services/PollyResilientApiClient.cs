using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AzureFunctions.Demo.Services;

// Slide 9 — Polly v8 ResiliencePipeline:
//   1) Retry: 3 reintentos, backoff exponencial con jitter (slide 6 —
//      el jitter evita que 100 funciones reintenten en el mismo segundo)
//   2) Circuit breaker: si fallan >=50% de >=4 llamadas en 10s, abre el
//      circuito 5s. Mientras está abierto, las llamadas fallan rápido
//      sin tocar el servicio (slide 9 — "no reintentar lo imposible").
//
// El orden importa: el retry envuelve al circuit breaker, así un circuito
// abierto NO consume los reintentos (BrokenCircuitException no se reintenta).
public sealed class PollyResilientApiClient : IResilientApiClient
{
    private readonly ResiliencePipeline _pipeline;

    public PollyResilientApiClient(ILogger<PollyResilientApiClient> logger)
        : this(logger, retryDelay: TimeSpan.FromMilliseconds(200))
    {
    }

    // Constructor con delay configurable: los tests usan delays mínimos
    // para no tardar segundos en validar el backoff.
    internal PollyResilientApiClient(
        ILogger<PollyResilientApiClient> logger,
        TimeSpan retryDelay,
        int breakerMinimumThroughput = 4,
        TimeSpan? breakDuration = null)
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // Solo reintentamos lo transitorio; un BrokenCircuitException
                // NO se reintenta (sería inútil — el circuito está abierto).
                ShouldHandle = new PredicateBuilder()
                    .Handle<ErrorTransitorioException>()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = retryDelay,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Reintento {Attempt} tras {Delay}ms ({Ex})",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.GetType().Name);
                    return ValueTask.CompletedTask;
                },
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<ErrorTransitorioException>()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>(),
                FailureRatio = 0.5,
                MinimumThroughput = breakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(5),
                OnOpened = args =>
                {
                    logger.LogError(
                        "Circuito ABIERTO {Duration}s (fallos sostenidos)",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Circuito CERRADO (servicio recuperado)");
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    public async Task<T> EjecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        try
        {
            return await _pipeline.ExecuteAsync(
                async token => await operacion(token), ct);
        }
        catch (BrokenCircuitException)
        {
            // Traducimos la excepción de Polly a la de dominio para que el
            // ErrorClassifier la trate como transitoria (el circuito reabrirá).
            throw new CircuitoAbiertoException(
                "El circuito está abierto: el servicio externo lleva demasiados fallos");
        }
    }
}
