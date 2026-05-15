using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

public class PollyResilientApiClientTests
{
    // minThroughput alto por defecto (10): una secuencia de retry normal
    // (≤4 ejecuciones del circuit breaker) NO debe abrir el circuito.
    // El test de "circuito abre" hace muchas más ejecuciones a propósito.
    private static PollyResilientApiClient New(
        int minThroughput = 10, TimeSpan? breakDuration = null)
        => new(
            NullLogger<PollyResilientApiClient>.Instance,
            retryDelay: TimeSpan.FromMilliseconds(1),  // backoff casi cero en tests
            breakerMinimumThroughput: minThroughput,
            breakDuration: breakDuration ?? TimeSpan.FromSeconds(30));

    [Fact]
    public async Task Operacion_Que_Tiene_Exito_A_La_Primera_Devuelve_El_Valor()
    {
        var client = New();

        var r = await client.EjecutarAsync(_ => Task.FromResult(42));

        Assert.Equal(42, r);
    }

    [Fact]
    public async Task Retry_Reintenta_Lo_Transitorio_Y_Acaba_Teniendo_Exito()
    {
        // Slide 6 — falla 2 veces (transitorio) y a la 3ª va bien.
        // El pipeline reintenta hasta 3 veces → debe terminar OK.
        var client = New();
        var intentos = 0;

        var r = await client.EjecutarAsync<string>(_ =>
        {
            intentos++;
            if (intentos < 3)
                throw new ErrorTransitorioException($"fallo transitorio {intentos}");
            return Task.FromResult("ok");
        });

        Assert.Equal("ok", r);
        Assert.Equal(3, intentos);
    }

    [Fact]
    public async Task Error_Permanente_No_Se_Reintenta()
    {
        // ErrorPermanente NO está en el ShouldHandle del retry → se
        // propaga al primer intento, sin reintentos.
        var client = New();
        var intentos = 0;

        await Assert.ThrowsAsync<ErrorPermanenteException>(() =>
            client.EjecutarAsync<string>(_ =>
            {
                intentos++;
                throw new ErrorPermanenteException("datos inválidos");
            }));

        Assert.Equal(1, intentos); // un solo intento, NO reintenta
    }

    [Fact]
    public async Task Circuito_Abre_Tras_Fallos_Sostenidos_Y_Lanza_CircuitoAbierto()
    {
        // Slide 9 — con fallos sostenidos el circuito abre; las llamadas
        // siguientes fallan rápido con CircuitoAbiertoException sin tocar
        // el servicio.
        var client = New(minThroughput: 2, breakDuration: TimeSpan.FromSeconds(30));

        var circuitoAbierto = false;
        for (var i = 0; i < 10 && !circuitoAbierto; i++)
        {
            try
            {
                await client.EjecutarAsync<string>(_ =>
                    throw new ErrorTransitorioException("servicio caído"));
            }
            catch (CircuitoAbiertoException)
            {
                circuitoAbierto = true;
            }
            catch (ErrorTransitorioException)
            {
                // aún cerrado o medio-abierto: sigue intentando
            }
        }

        Assert.True(circuitoAbierto,
            "El circuito debería haberse abierto tras fallos sostenidos");
    }
}
