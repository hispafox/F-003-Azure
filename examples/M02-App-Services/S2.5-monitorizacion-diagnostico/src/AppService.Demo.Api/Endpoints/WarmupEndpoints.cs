using AppService.Demo.Api.Services;

namespace AppService.Demo.Api.Endpoints;

public static class WarmupEndpoints
{
    public static IEndpointRouteBuilder MapWarmup(this IEndpointRouteBuilder app)
    {
        // Slides 16 y 29 — App Service llama a este endpoint antes de redirigir
        // tráfico durante el swap si configuras WEBSITE_SWAP_WARMUP_PING_PATH=/warmup
        // en App Settings. Devuelve 200 cuando la app está caliente, 503 si alguna
        // dependencia falla — App Service aborta el swap si no recibe 200.
        app.MapGet("/warmup", async (
            DependencyChecks checks,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Warmup requested on slot {Slot}",
                Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME") ?? "local");

            var result = await checks.RunAsync(ct);

            return result.AllOk
                ? Results.Ok(new { status = "warm", checks = result.Checks })
                : Results.Json(
                    new { status = "cold", checks = result.Checks },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return app;
    }
}
