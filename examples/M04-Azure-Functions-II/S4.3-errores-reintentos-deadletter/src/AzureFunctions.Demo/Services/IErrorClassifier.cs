using System.Text.Json;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 3/13 — el corazón de la estrategia de errores: dada una excepción,
// decidir si conviene reintentar, mandar a dead-letter o tratarla como
// desconocida. Lógica pura → 100% testeable.
public interface IErrorClassifier
{
    TipoError Clasificar(Exception ex);
}

public sealed class ErrorClassifier : IErrorClassifier
{
    public TipoError Clasificar(Exception ex) => ex switch
    {
        // Errores que NO se arreglan reintentando → dead-letter inmediato.
        ErrorPermanenteException => TipoError.Permanente,
        JsonException => TipoError.Permanente,            // payload malformado
        ArgumentException => TipoError.Permanente,         // validación
        InvalidOperationException => TipoError.Permanente, // regla de negocio

        // Errores que típicamente se resuelven solos → reintentar con backoff.
        ErrorTransitorioException => TipoError.Transitorio,
        TimeoutException => TipoError.Transitorio,
        TaskCanceledException => TipoError.Transitorio,    // timeout de HttpClient
        HttpRequestException http when EsTransitorioHttp(http) => TipoError.Transitorio,
        CircuitoAbiertoException => TipoError.Transitorio, // el circuito reabrirá

        // Cualquier otra cosa: la tratamos como desconocida (log critical +
        // reintentar — podría ser un transitorio que no hemos catalogado).
        _ => TipoError.Desconocido,
    };

    private static bool EsTransitorioHttp(HttpRequestException ex)
        => ex.StatusCode is
            System.Net.HttpStatusCode.RequestTimeout or       // 408
            System.Net.HttpStatusCode.TooManyRequests or      // 429
            System.Net.HttpStatusCode.BadGateway or           // 502
            System.Net.HttpStatusCode.ServiceUnavailable or   // 503
            System.Net.HttpStatusCode.GatewayTimeout          // 504
            // Sin StatusCode (fallo de conexión) también es transitorio.
            or null;
}
