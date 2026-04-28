using AppService.Demo.Api.Telemetry;

namespace AppService.Demo.Api.Endpoints;

public sealed record LogRequest(string Message);

public static class LogDemoEndpoints
{
    public static IEndpointRouteBuilder MapLogDemo(this IEndpointRouteBuilder app)
    {
        // Slides 23, 25 — Endpoint que recibe un mensaje libre y lo loguea
        // tras pasarlo por PiiScrubber. Devuelve también el resultado para
        // poder verificarlo desde un cliente. Pensado para escenificar el
        // riesgo de loguear PII y la mitigación correcta.
        app.MapPost("/demo/log", (LogRequest body, ILogger<Program> logger) =>
        {
            var original = body.Message ?? string.Empty;
            var safe = PiiScrubber.Scrub(original);

            // Slide 23 — structured logging. {SafeMessage} y {OriginalLength}
            // viajan como campos separados a App Insights.
            logger.LogInformation(
                "Mensaje recibido (longitud {OriginalLength}): {SafeMessage}",
                original.Length, safe);

            return Results.Ok(new
            {
                originalLength = original.Length,
                scrubbed = safe,
                redactionsApplied = original != safe
            });
        });

        return app;
    }
}
