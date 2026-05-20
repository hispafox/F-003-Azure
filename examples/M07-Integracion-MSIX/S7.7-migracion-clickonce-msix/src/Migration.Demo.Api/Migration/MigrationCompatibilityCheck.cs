namespace Migration.Demo.Api.Migration;

// Slide 3 — comportamientos detectados en la app que afectan a la
// migración a MSIX.
public enum ComportamientoApp
{
    KernelDriver,                   // ✗ bloqueador
    WindowsService,                 // ⚠ se puede con PSF
    ComServerNoEnManifest,          // ⚠ se puede declarar
    EscribeProgramFilesOWindows,    // ✗ bloqueador
    EscribeHKLM,                    // ⚠ PSF puede redirigir
    BuscaDllsEnPathGlobal,          // ⚠ PSF needed
    Wpf,                             // ✓ ok
    WinForms,                        // ✓ ok
    ConsoleApp,                     // ✓ ok
    UsaFilesystemDelUsuario,        // ✓ ok
    UsaRegistroDelUsuario,          // ✓ ok (virtualizado)
    LlamadasHttp,                   // ✓ ok
}

public enum NivelRiesgo { Ok, Precaucion, Bloqueador }

public sealed record EvaluacionCompatibilidad(
    NivelRiesgo Riesgo,
    IReadOnlyList<string> Hallazgos,
    bool RequierePsf);

// Slides 3, 12 — clasifica el riesgo de migrar una app según los
// comportamientos detectados. Lógica pura.
public static class MigrationCompatibilityCheck
{
    private static readonly HashSet<ComportamientoApp> Bloqueadores =
    [
        ComportamientoApp.KernelDriver,
        ComportamientoApp.EscribeProgramFilesOWindows,
    ];

    private static readonly HashSet<ComportamientoApp> RequierenPsf =
    [
        ComportamientoApp.WindowsService,
        ComportamientoApp.EscribeHKLM,
        ComportamientoApp.BuscaDllsEnPathGlobal,
    ];

    private static readonly Dictionary<ComportamientoApp, string> Descripciones = new()
    {
        [ComportamientoApp.KernelDriver] =
            "Drivers de kernel: no son posibles en MSIX (slide 3).",
        [ComportamientoApp.WindowsService] =
            "Windows service: posible con PSF, pero añade complejidad (slide 3/12).",
        [ComportamientoApp.ComServerNoEnManifest] =
            "COM server: declárarlo en el manifest (slide 3).",
        [ComportamientoApp.EscribeProgramFilesOWindows] =
            "Escritura directa en C:\\Windows o C:\\Program Files: incompatible con el sandbox (slide 3).",
        [ComportamientoApp.EscribeHKLM] =
            "Escritura en HKLM: virtualizada por defecto; PSF puede redirigir si la app espera HKLM real (slide 12).",
        [ComportamientoApp.BuscaDllsEnPathGlobal] =
            "Búsqueda de DLLs en PATH global: PSF puede redirigir (slide 12).",
        [ComportamientoApp.Wpf] = "WPF: compatible con MSIX (slide 3).",
        [ComportamientoApp.WinForms] = "WinForms: compatible con MSIX (slide 3).",
        [ComportamientoApp.ConsoleApp] = "Console: compatible con MSIX (slide 3).",
        [ComportamientoApp.UsaFilesystemDelUsuario] =
            "Acceso al filesystem del usuario: compatible (slide 3).",
        [ComportamientoApp.UsaRegistroDelUsuario] =
            "Registry del usuario: virtualizado automáticamente (slide 3).",
        [ComportamientoApp.LlamadasHttp] = "Llamadas HTTP/API: compatibles (slide 3).",
    };

    public static EvaluacionCompatibilidad Evaluar(
        IReadOnlyList<ComportamientoApp> comportamientos)
    {
        ArgumentNullException.ThrowIfNull(comportamientos);

        var hallazgos = comportamientos
            .Distinct()
            .Select(c => Descripciones.TryGetValue(c, out var d) ? d : c.ToString())
            .ToList();

        var nivel = comportamientos.Any(Bloqueadores.Contains)
            ? NivelRiesgo.Bloqueador
            : comportamientos.Any(RequierenPsf.Contains)
                ? NivelRiesgo.Precaucion
                : NivelRiesgo.Ok;

        bool psf = comportamientos.Any(RequierenPsf.Contains);

        return new EvaluacionCompatibilidad(nivel, hallazgos, psf);
    }
}
