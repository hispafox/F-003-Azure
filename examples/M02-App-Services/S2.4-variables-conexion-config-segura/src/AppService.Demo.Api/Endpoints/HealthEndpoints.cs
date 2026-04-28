using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace AppService.Demo.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        // Slide 13 (S2.1) — endpoint simple que App Service consulta
        app.MapHealthChecks("/health");

        // Slide 21 — variante con response writer JSON: misma información pero
        // legible para humanos y para dashboards. App Service sigue apuntando a
        // /health (texto plano) para no inflar el tráfico del health check.
        app.MapHealthChecks("/health/details", new HealthCheckOptions
        {
            ResponseWriter = static async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = new
                {
                    status = report.Status.ToString(),
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        durationMs = entry.Value.Duration.TotalMilliseconds
                    })
                };

                await context.Response.WriteAsJsonAsync(payload);
            }
        });

        return app;
    }
}
