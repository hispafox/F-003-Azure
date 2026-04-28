using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class VersionEndpoints
{
    public static IEndpointRouteBuilder MapVersion(this IEndpointRouteBuilder app)
    {
        // Slide 9 — `version` viaja con el código (NO sticky). Tras un swap,
        // /version cambia inmediatamente: es la prueba más visual de que el
        // swap funcionó. `environmentLabel` se queda en su slot (sticky), así
        // que en producción siempre dirá "production" aunque el código sea nuevo.
        app.MapGet("/version", (IOptions<AppOptions> options) =>
            Results.Ok(new
            {
                version = options.Value.Version,
                slotName = Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME") ?? "local",
                environmentLabel = options.Value.EnvironmentLabel,
                timestamp = DateTimeOffset.UtcNow
            }));

        return app;
    }
}
