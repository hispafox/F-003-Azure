namespace Bonus.SetupAzure.Demo.Api.Setup;

public enum NivelRiesgoSettings { Critico, Alto, Medio, Bajo }

public sealed record HallazgoSettings(
    NivelRiesgoSettings Nivel, string Comprobacion, string Mensaje);

public sealed record InformeSettings(
    bool Seguro,
    IReadOnlyList<HallazgoSettings> Hallazgos);

public sealed record EscenarioSettings(
    IReadOnlyList<string> Allow,
    IReadOnlyList<string> Deny,
    string? Model = null);

// Slide 7/9 — validador del `settings.json` (sobre todo permissions).
// Lógica pura. Detecta los riesgos típicos:
//   - `allow Bash(* *)` o `allow Bash(*)` muy amplios → Critico
//   - sin `deny` de comandos destructivos (rm -rf, az group delete) → Alto
//   - sin exclude de archivos sensibles (.env, .pfx, local.settings.json) → Alto
//   - sin modelo declarado → Medio (warning)
public static class SettingsPermissionsValidator
{
    // Comandos destructivos que SIEMPRE deberían estar en deny.
    private static readonly string[] DenyImprescindibles =
    [
        "rm -rf",
        "az group delete",
        "az resource delete",
        "drop database",
    ];

    // Archivos que NUNCA deberían poder leerse desde Claude.
    private static readonly string[] LecturaProhibida =
    [
        "*.env",
        "*.pfx",
        "*.key",
        "local.settings.json",
    ];

    public static InformeSettings Validar(EscenarioSettings s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var hallazgos = new List<HallazgoSettings>();

        // 1) Allow demasiado amplio → Critico.
        foreach (var allow in s.Allow)
        {
            if (string.Equals(allow, "Bash(*)", StringComparison.OrdinalIgnoreCase)
                || string.Equals(allow, "Bash(* *)", StringComparison.OrdinalIgnoreCase))
                hallazgos.Add(new(NivelRiesgoSettings.Critico,
                    $"Allow demasiado amplio: `{allow}`",
                    "Permite cualquier comando shell sin restricciones. " +
                    "Restringe a `Bash(dotnet *)`, `Bash(az *)`, etc. (slide 7)."));

            if (string.Equals(allow, "Write(**)", StringComparison.OrdinalIgnoreCase)
                || string.Equals(allow, "Edit(**)", StringComparison.OrdinalIgnoreCase))
                hallazgos.Add(new(NivelRiesgoSettings.Critico,
                    $"Allow demasiado amplio: `{allow}`",
                    "Permite escribir en CUALQUIER ruta. Limita a `Write(src/**)`, " +
                    "`Write(infrastructure/**)`, etc. (slide 7)."));
        }

        // 2) Falta deny de comandos destructivos → Alto por cada uno.
        foreach (var cmd in DenyImprescindibles)
        {
            bool tieneDeny = s.Deny.Any(d =>
                d.Contains(cmd, StringComparison.OrdinalIgnoreCase));
            if (!tieneDeny)
                hallazgos.Add(new(NivelRiesgoSettings.Alto,
                    $"Falta `deny` para `{cmd}`",
                    $"Añade `\"Bash({cmd} *)\"` a `deny` para que Claude nunca " +
                    "lo ejecute aunque parezca razonable (slide 7)."));
        }

        // 3) Falta deny de lecturas prohibidas → Alto por cada uno.
        foreach (var patron in LecturaProhibida)
        {
            bool tieneDeny = s.Deny.Any(d =>
                d.Contains(patron, StringComparison.OrdinalIgnoreCase));
            if (!tieneDeny)
                hallazgos.Add(new(NivelRiesgoSettings.Alto,
                    $"Falta `deny` para lectura de `{patron}`",
                    $"Añade `\"Read(**/{patron})\"` a `deny` para excluir " +
                    "archivos sensibles (slide 7)."));
        }

        // 4) Sin modelo declarado → Medio (warning).
        if (string.IsNullOrWhiteSpace(s.Model))
            hallazgos.Add(new(NivelRiesgoSettings.Medio,
                "Sin `model` declarado",
                "Declara el modelo del equipo (`claude-sonnet-4-6` por defecto) en " +
                "`settings.json` para que todos usen la misma generación (slide 7)."));

        bool seguro = !hallazgos.Any(h =>
            h.Nivel is NivelRiesgoSettings.Critico or NivelRiesgoSettings.Alto);

        return new InformeSettings(seguro, hallazgos);
    }
}
