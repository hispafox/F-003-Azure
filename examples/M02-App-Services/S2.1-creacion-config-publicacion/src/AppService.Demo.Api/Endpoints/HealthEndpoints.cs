namespace AppService.Demo.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        // Slide 13 — App Service consulta este endpoint cuando configuras
        // "Health check" en Configuration → General. 200 ⇒ instancia sana,
        // cualquier otra cosa ⇒ App Service la reinicia.
        app.MapHealthChecks("/health");
        return app;
    }
}
