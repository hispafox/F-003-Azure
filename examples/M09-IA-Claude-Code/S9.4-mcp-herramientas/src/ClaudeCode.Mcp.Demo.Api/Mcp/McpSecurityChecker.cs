using System.Text.RegularExpressions;

namespace ClaudeCode.Mcp.Demo.Api.Mcp;

public enum NivelRiesgo { Critico, Alto, Medio, Bajo }

public sealed record HallazgoSeguridad(
    NivelRiesgo Riesgo, string Server, string Causa, string Mitigacion);

public sealed record InformeSeguridad(
    bool Seguro,
    IReadOnlyList<HallazgoSeguridad> Hallazgos,
    int Criticos,
    int Altos);

// Slide 9 — security checker del `claude_desktop_config.json`. Lógica
// pura. Detecta:
//   - tokens / API keys hardcoded en `env` (deben venir del entorno)
//   - servers con scope demasiado amplio (filesystem en `/`, `$HOME`)
//   - tokens sin marca de rotación (criterio del equipo)
public static partial class McpSecurityChecker
{
    // Patrones de valores que pintan como secretos reales (no como
    // referencias `${VAR}` o `$VAR`).
    [GeneratedRegex(@"^(ghp_|github_pat_|ghu_|ghs_|ghr_)[A-Za-z0-9_]{20,}$")]
    private static partial Regex TokenGithubRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{52}$")] // PAT de ADO clásico (~52 chars base64ish)
    private static partial Regex TokenAdoRegex();

    [GeneratedRegex(@"^xoxb-[A-Za-z0-9-]+$")] // Slack bot tokens
    private static partial Regex TokenSlackRegex();

    [GeneratedRegex(@"^sk-[A-Za-z0-9_\-]{20,}$")]
    private static partial Regex SecretKeyRegex();

    private static readonly string[] NombresEnvSensibles =
    [
        "TOKEN", "PAT", "API_KEY", "SECRET", "PASSWORD",
        "CONNECTION_STRING", "WEBHOOK", "BEARER",
    ];

    public static InformeSeguridad Comprobar(McpConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var hallazgos = new List<HallazgoSeguridad>();

        foreach (var sv in config.Servers)
        {
            foreach (var (key, value) in sv.Env)
            {
                bool esSensible = NombresEnvSensibles.Any(s =>
                    key.Contains(s, StringComparison.OrdinalIgnoreCase));

                if (esSensible && ParecePlanoReal(value))
                    hallazgos.Add(new(
                        NivelRiesgo.Critico,
                        sv.Nombre,
                        $"`{key}` contiene un valor que parece un secreto real, " +
                            "no una referencia a variable de entorno.",
                        $"Sustituye por `${{{key}}}` o `$env:{key}` y exporta el " +
                            "valor desde el entorno (slide 9)."));
                else if (esSensible && string.IsNullOrWhiteSpace(value))
                    hallazgos.Add(new(
                        NivelRiesgo.Alto,
                        sv.Nombre,
                        $"`{key}` está vacío — el server no podrá autenticar.",
                        "Define la variable en el entorno o saca el server de la config."));
            }

            // Filesystem server con paths demasiado amplios.
            if (string.Equals(sv.Nombre, "filesystem", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var arg in sv.Args)
                {
                    if (PareceRaiz(arg))
                        hallazgos.Add(new(
                            NivelRiesgo.Critico,
                            sv.Nombre,
                            $"`filesystem` con path demasiado amplio: `{arg}`.",
                            "Restringe a la carpeta del proyecto (`/home/dev/projects/<repo>`)."));
                }
            }

            // Tokens de ADO/GitHub que no usan referencia a env.
            // Aviso medio si el server es de write y el equipo no marca
            // rotación cada 90 días (lo dejamos como recordatorio).
            if (sv.Nombre.Contains("github", StringComparison.OrdinalIgnoreCase)
                || sv.Nombre.Contains("azure-devops", StringComparison.OrdinalIgnoreCase))
            {
                hallazgos.Add(new(
                    NivelRiesgo.Medio,
                    sv.Nombre,
                    "Servers de Git con tokens deben rotarse cada 90 días.",
                    "Documenta el calendario de rotación en `docs/mcp-rotation.md`."));
            }
        }

        int criticos = hallazgos.Count(h => h.Riesgo == NivelRiesgo.Critico);
        int altos = hallazgos.Count(h => h.Riesgo == NivelRiesgo.Alto);
        return new InformeSeguridad(criticos == 0 && altos == 0, hallazgos, criticos, altos);
    }

    private static bool ParecePlanoReal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        // Referencias de variable son OK: `${VAR}`, `$VAR`, `$env:VAR`.
        var trimmed = value.Trim();
        if (trimmed.StartsWith("${", StringComparison.Ordinal)) return false;
        if (trimmed.StartsWith("$env:", StringComparison.Ordinal)) return false;
        if (trimmed.StartsWith("$", StringComparison.Ordinal) && !trimmed.Contains(' ', StringComparison.Ordinal))
            return false;

        return TokenGithubRegex().IsMatch(trimmed)
            || TokenAdoRegex().IsMatch(trimmed)
            || TokenSlackRegex().IsMatch(trimmed)
            || SecretKeyRegex().IsMatch(trimmed);
    }

    private static bool PareceRaiz(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var p = path.Trim();
        return p == "/" || p == "~" || p == "$HOME"
            || p.Equals("C:\\", StringComparison.Ordinal)
            || p.Equals("/home", StringComparison.Ordinal)
            || p.Equals("/Users", StringComparison.Ordinal);
    }
}
