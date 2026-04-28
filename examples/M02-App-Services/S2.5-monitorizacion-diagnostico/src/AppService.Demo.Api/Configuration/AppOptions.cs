using System.ComponentModel.DataAnnotations;

namespace AppService.Demo.Api.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    [Required(AllowEmptyStrings = false)]
    public string Greeting { get; init; } = "Hola desde App Service";

    public bool Healthy { get; init; } = true;

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();

    public string Version { get; init; } = "0.0.0";

    [Required(AllowEmptyStrings = false)]
    public string EnvironmentLabel { get; init; } = "local";

    public string DbConnectionLabel { get; init; } = "local-db";
    public string AppInsightsLabel { get; init; } = "local-insights";

    // Slide 7 — Connection string. En Azure llega como App Setting con prefijo
    // o como Key Vault Reference. En local viene de appsettings/User Secrets.
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } =
        "Server=localhost;Database=local;Integrated Security=true";

    // Slide 9 — Secret típico que en Azure debe ser Key Vault Reference.
    // Si llega literal "@Microsoft.KeyVault(...)", la KV ref no se resolvió:
    // probablemente le falta rol al MI o el secret no existe.
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = "local-api-key-placeholder";

    [Range(1, 60)]
    public int RequestTimeoutSeconds { get; init; } = 30;

    [Url]
    public string ExternalApiBaseUrl { get; init; } = "https://api.github.com";
}
