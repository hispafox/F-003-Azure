namespace Deploy.Demo.Api.Deploy;

public sealed record HealthAttempt(int Intento, int StatusObservado);

public sealed record ResultadoHealthCheck(
    bool Pasa,
    int IntentosUsados,
    string Razon);

public sealed record SmokeRequest(string Endpoint, int StatusObservado);

public sealed record ResultadoSmoke(
    bool Pasa,
    IReadOnlyList<string> EndpointsOk,
    IReadOnlyList<string> EndpointsFallidos);

// Slide 9 — retry con backoff de health checks post-deploy + smoke
// test funcional (varios endpoints). Lógica pura: recibe los códigos
// observados (los hace el pipeline real con curl).
public static class HealthCheckEvaluator
{
    // Pasa si CUALQUIER intento observa el statusEsperado dentro del
    // número máximo de intentos. Modelo del bucle `for i in 1..N` del
    // YAML de la slide 9.
    public static ResultadoHealthCheck Evaluar(
        int statusEsperado, int maxIntentos,
        IReadOnlyList<HealthAttempt> intentos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxIntentos);
        ArgumentNullException.ThrowIfNull(intentos);

        var ordenados = intentos.OrderBy(x => x.Intento).Take(maxIntentos).ToList();
        for (int i = 0; i < ordenados.Count; i++)
            if (ordenados[i].StatusObservado == statusEsperado)
                return new ResultadoHealthCheck(true, i + 1,
                    $"✓ Health OK en el intento {i + 1}.");

        int ultimo = ordenados.Count > 0 ? ordenados[^1].StatusObservado : 0;
        return new ResultadoHealthCheck(false, ordenados.Count,
            $"✗ Health check falló tras {ordenados.Count} intentos " +
            $"(último HTTP {ultimo}).");
    }

    // Slide 9 — smoke test: TODOS los endpoints deben responder 2xx.
    public static ResultadoSmoke EvaluarSmoke(IReadOnlyList<SmokeRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var ok = new List<string>();
        var ko = new List<string>();
        foreach (var r in requests)
        {
            if (r.StatusObservado is >= 200 and < 300) ok.Add(r.Endpoint);
            else ko.Add($"{r.Endpoint} (HTTP {r.StatusObservado})");
        }
        return new ResultadoSmoke(ko.Count == 0, ok, ko);
    }
}
