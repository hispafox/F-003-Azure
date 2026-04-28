using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapInfo(this IEndpointRouteBuilder app)
    {
        // Slides 8, 9, 14 — /info muestra qué slot atiende la petición y separa
        // las settings que viajan con el código (no sticky) de las que se quedan
        // en el slot (sticky). Tras un swap, lo etiquetado "sticky" no cambia;
        // lo etiquetado "travels-with-code" sí.
        app.MapGet("/info", (IOptions<AppOptions> options) =>
        {
            var slotName = Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME") ?? "local";

            return Results.Ok(new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                dotnetVersion = Environment.Version.ToString(),
                instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local",
                siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "local",
                slotName,
                resourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP") ?? "local",
                travelsWithCode = new
                {
                    version = options.Value.Version,
                    greeting = options.Value.Greeting,
                    allowedOrigins = options.Value.AllowedOrigins
                },
                stickyToSlot = new
                {
                    environmentLabel = options.Value.EnvironmentLabel,
                    dbConnectionLabel = options.Value.DbConnectionLabel,
                    appInsightsLabel = options.Value.AppInsightsLabel
                }
            });
        });

        return app;
    }
}
