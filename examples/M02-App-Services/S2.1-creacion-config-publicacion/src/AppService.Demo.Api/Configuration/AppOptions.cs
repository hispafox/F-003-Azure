using System.ComponentModel.DataAnnotations;

namespace AppService.Demo.Api.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    [Required(AllowEmptyStrings = false)]
    public string Greeting { get; init; } = "Hola desde App Service";

    public bool Healthy { get; init; } = true;

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
