using System.ComponentModel.DataAnnotations;

namespace AppService.Practica.Api.Configuration;

public sealed class PracticaOptions
{
    public const string SectionName = "Practica";

    // Slide 7 — En la práctica didáctica, "version" y "novedad" simulan el
    // CÓDIGO que cambia entre v1 y v2. En Azure llegan como App Settings
    // normales (no sticky), así que viajan con el slot durante el swap.
    [Required(AllowEmptyStrings = false)]
    public string Version { get; init; } = "1.0";

    [Required(AllowEmptyStrings = false)]
    public string Novedad { get; init; } = "Hello World";

    // Slide 6 — "NotaEntorno" se configura como SLOT setting (sticky); cada
    // slot tiene la suya y NO viaja con el swap.
    public string NotaEntorno { get; init; } = "no definida";
}
