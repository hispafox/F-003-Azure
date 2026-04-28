using AppService.Demo.Api.Services;

namespace AppService.Demo.Api.Endpoints;

public static class DemoErrorEndpoints
{
    public static IEndpointRouteBuilder MapDemoErrors(this IEndpointRouteBuilder app)
    {
        // Slides 4, 12, 17 — Endpoint para escenificar el dashboard de errores y
        // las alertas en clase. Cada `type` reproduce un fallo distinto:
        //   500              → Results.Problem 500 explícito
        //   exception        → InvalidOperationException (telemetría como exception)
        //   slow             → 5 s de Task.Delay (dispara alerta de latencia > 3s)
        //   dependency-fail  → HttpClient a un host inalcanzable (failed dependency)
        app.MapGet("/demo/error", async (
            string type,
            ExternalApiClient client,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            switch (type)
            {
                case "500":
                    logger.LogError("Demo: emitiendo 500 controlado");
                    return Results.Problem(
                        title: "Simulated server error",
                        detail: "Type 500 triggered on purpose for monitoring demo.",
                        statusCode: StatusCodes.Status500InternalServerError);

                case "exception":
                    logger.LogError("Demo: lanzando InvalidOperationException");
                    throw new InvalidOperationException("Simulated exception for monitoring demo");

                case "slow":
                    logger.LogWarning("Demo: respuesta lenta intencional (5 s)");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    return Results.Ok(new { type = "slow", waitedMs = 5_000 });

                case "dependency-fail":
                    logger.LogWarning("Demo: forzando fallo de dependency");
                    using (var bad = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
                    {
                        try
                        {
                            _ = await bad.GetAsync("https://does-not-exist.invalid/health", ct);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Dependency call failed (esperado en este endpoint)");
                            return Results.Problem(
                                title: "Dependency failed",
                                detail: ex.Message,
                                statusCode: StatusCodes.Status502BadGateway);
                        }
                    }
                    return Results.Ok(new { type = "dependency-fail" });

                default:
                    return Results.BadRequest(new
                    {
                        error = "Unknown type",
                        allowed = new[] { "500", "exception", "slow", "dependency-fail" }
                    });
            }
        });

        return app;
    }
}
