using Microsoft.FeatureManagement;

namespace AppService.Demo.Api.Endpoints;

public static class FeatureFlagEndpoints
{
    public static IEndpointRouteBuilder MapFeatureFlags(this IEndpointRouteBuilder app)
    {
        // Slides 11 y 16 — Feature flag con Microsoft.FeatureManagement.
        // El valor se lee de la sección "FeatureManagement" de la configuración:
        //   - appsettings.json → "FeatureManagement": { "NewUI": false }
        //   - App Settings    → "FeatureManagement__NewUI=true"
        // Cambiar este App Setting reinicia la app y aplica el nuevo valor.
        app.MapGet("/features/new-ui", async (IFeatureManager features) =>
        {
            var enabled = await features.IsEnabledAsync("NewUI");
            return Results.Ok(new
            {
                feature = "NewUI",
                enabled,
                payload = enabled
                    ? new { version = "v2", message = "Bienvenido a la nueva UI" }
                    : new { version = "v1", message = "UI clásica" }
            });
        });

        return app;
    }
}
