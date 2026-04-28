using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class HelloEndpoints
{
    public static IEndpointRouteBuilder MapHello(this IEndpointRouteBuilder app)
    {
        // Slide 4 — Con varias instancias activas, este endpoint permite ver a cuál
        // ha llegado la petición. WEBSITE_INSTANCE_ID lo inyecta App Service.
        app.MapGet("/", (IOptions<AppOptions> options, ILogger<Program> logger) =>
        {
            var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local";
            logger.LogInformation("Hello endpoint hit on instance {Instance}", instanceId);

            return Results.Ok(new
            {
                greeting = options.Value.Greeting,
                machineName = Environment.MachineName,
                instanceId,
                timestamp = DateTimeOffset.UtcNow
            });
        });

        return app;
    }
}
