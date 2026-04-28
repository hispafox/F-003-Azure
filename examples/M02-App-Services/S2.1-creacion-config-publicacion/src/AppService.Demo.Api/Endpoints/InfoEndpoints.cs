using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapInfo(this IEndpointRouteBuilder app)
    {
        // Slides 12 y 14 — En producción la app lee configuración desde
        // Configuration → Application settings. Esas settings llegan como variables
        // de entorno usando "__" como separador de secciones (AppOptions__Greeting).
        app.MapGet("/info", (IOptions<AppOptions> options) =>
        {
            return Results.Ok(new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                dotnetVersion = Environment.Version.ToString(),
                instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local",
                siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "local",
                resourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP") ?? "local",
                appOptions = new
                {
                    greeting = options.Value.Greeting,
                    healthy = options.Value.Healthy,
                    allowedOrigins = options.Value.AllowedOrigins
                }
            });
        });

        return app;
    }
}
