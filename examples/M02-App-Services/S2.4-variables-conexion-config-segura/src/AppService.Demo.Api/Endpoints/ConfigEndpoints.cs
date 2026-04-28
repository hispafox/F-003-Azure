using AppService.Demo.Api.Configuration;

namespace AppService.Demo.Api.Endpoints;

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfig(this IEndpointRouteBuilder app)
    {
        // Slide 28 — /config expone toda la configuración aplicada con scrubbing
        // por nombre de clave. Útil para diagnóstico sin filtrar secretos.
        app.MapGet("/config", (IConfiguration config) =>
        {
            var scrubbed = ConfigScrubber.ScrubAll(config);
            return Results.Ok(scrubbed);
        });

        // Slide 7 — /connection muestra solo los campos seguros de la connection
        // string (Server, Database, Encrypt). Permite verificar a qué BD se está
        // conectando sin filtrar la password.
        app.MapGet("/connection", (IConfiguration config) =>
        {
            // Probamos primero ConnectionStrings:Default (slot setting clásico),
            // luego AppOptions:ConnectionString (nuestro POCO).
            var connStr = config.GetConnectionString("Default")
                          ?? config["AppOptions:ConnectionString"]
                          ?? string.Empty;

            var fields = ConnectionStringInspector.ExtractSafeFields(connStr);

            return Results.Ok(new
            {
                hasConnectionString = !string.IsNullOrEmpty(connStr),
                isKeyVaultReferenceLiteral = connStr.StartsWith("@Microsoft.KeyVault", StringComparison.Ordinal),
                safeFields = fields
            });
        });

        return app;
    }
}
