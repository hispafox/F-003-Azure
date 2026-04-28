using AppService.Demo.Api.Services;

namespace AppService.Demo.Api.Endpoints;

public static class LoadEndpoints
{
    public static IEndpointRouteBuilder MapLoad(this IEndpointRouteBuilder app)
    {
        // Slides 5, 6, 7 — /load/cpu?ms=N quema CPU N milisegundos. Bombardeando
        // este endpoint en bucle (scripts/07-load-test.sh) la métrica
        // CpuPercentage del plan supera el umbral del autoscale y se añaden
        // instancias. Pensado SOLO para demo en clase.
        app.MapGet("/load/cpu", (
            CpuLoadGenerator generator,
            ILogger<Program> logger,
            int ms = 1_000,
            CancellationToken ct = default) =>
        {
            if (ms < 1 || ms > 60_000)
            {
                return Results.BadRequest(new { error = "ms must be between 1 and 60000" });
            }

            logger.LogInformation("CPU burn requested: {Ms} ms on instance {Instance}",
                ms, Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local");

            var primes = generator.BurnCpu(TimeSpan.FromMilliseconds(ms), ct);

            return Results.Ok(new
            {
                generatedMs = ms,
                primesFound = primes,
                instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local"
            });
        });

        return app;
    }
}
