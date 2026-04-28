using System.ComponentModel.DataAnnotations;

namespace MiPrimeraWebApp.Configuration;

// Slide 14 — Settings que se cambian sin redesplegar via App Settings de Azure.
// Sección "Saludo" en appsettings.json; en App Service llegan como
// Saludo__Base, Saludo__MaxLength.
public sealed class SaludoOptions
{
    public const string SectionName = "Saludo";

    [Required(AllowEmptyStrings = false)]
    public string Base { get; init; } = "Hola,";

    [Range(1, 200)]
    public int MaxLength { get; init; } = 50;
}
