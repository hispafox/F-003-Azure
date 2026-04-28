using System.ComponentModel.DataAnnotations;

namespace AppService.Demo.Api.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    [Required(AllowEmptyStrings = false)]
    public string Greeting { get; init; } = "Hola desde App Service";

    public bool Healthy { get; init; } = true;

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();

    // Slide 9 — VIAJA con el código (NO sticky). Cambia tras un swap; útil para
    // verificar visualmente que la nueva versión está sirviendo en producción.
    public string Version { get; init; } = "0.0.0";

    // Slides 8 y 9 — Estos labels deben configurarse como "Slot setting" (sticky)
    // en App Service. Aquí sólo son strings ilustrativos: en una app real
    // representarían connection strings, App Insights keys, etc.
    public string EnvironmentLabel { get; init; } = "local";
    public string DbConnectionLabel { get; init; } = "local-db";
    public string AppInsightsLabel { get; init; } = "local-insights";
}
