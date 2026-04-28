using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapInfo(this IEndpointRouteBuilder app)
    {
        // Slides 8, 9, 28 — /info muestra slot + settings, separando lo que
        // viaja con el código de lo sticky, y SCRUBBING los valores sensibles
        // (ConnectionString, ApiKey) por nombre de clave.
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
                    allowedOrigins = options.Value.AllowedOrigins,
                    externalApiBaseUrl = options.Value.ExternalApiBaseUrl,
                    requestTimeoutSeconds = options.Value.RequestTimeoutSeconds
                },
                stickyToSlot = new
                {
                    environmentLabel = options.Value.EnvironmentLabel,
                    dbConnectionLabel = options.Value.DbConnectionLabel,
                    appInsightsLabel = options.Value.AppInsightsLabel,
                    connectionString = ConfigScrubber.Scrub("ConnectionString", options.Value.ConnectionString),
                    apiKey = ConfigScrubber.Scrub("ApiKey", options.Value.ApiKey)
                }
            });
        });

        return app;
    }
}
