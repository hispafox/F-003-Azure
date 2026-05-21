namespace Bonus.SkillsAzure.Demo.Api.Skills;

public sealed record InformeAntiPatrones(
    bool Limpio,
    IReadOnlyList<HallazgoSkill> Hallazgos);

// Slide 17 — detector de los anti-patrones del SKILL.md. Lógica pura.
// Cubre los DON'Ts:
//   #2 skill enorme (> 500 líneas) → Advertencia
//   #3 skill que solicita credenciales → Error
//   #5 tools sin restringir (Bash(*), Write sin scope) → Advertencia
// (#1 duplica CLAUDE.md y #4 skills contradictorios necesitan contexto
//  externo; no se detectan sobre un único SKILL.md aislado).
public static class SkillAntiPatternDetector
{
    private const int MaxLineas = 500;

    // Patrones que delatan que el skill pide/embebe credenciales.
    private static readonly string[] Credenciales =
    [
        "password=", "apikey=", "api key:", "client secret",
        "connectionstring=", "connection string:", "sk-ant-",
    ];

    // Tools demasiado amplios (rompen el menor privilegio del skill).
    private static readonly string[] ToolsAmplios =
    [
        "Bash(*)", "Bash", "Write(**)", "Edit(**)",
    ];

    public static InformeAntiPatrones Detectar(string skillMd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillMd);

        var hallazgos = new List<HallazgoSkill>();
        var lower = skillMd.ToLowerInvariant();

        // #2 — skill enorme.
        int lineas = skillMd.Replace("\r\n", "\n").Split('\n').Length;
        if (lineas > MaxLineas)
            hallazgos.Add(new(SeveridadSkill.Advertencia, "tamaño",
                $"El skill ocupa {lineas} líneas (> {MaxLineas}). Pártelo en varios " +
                "skills o usa archivos de apoyo (CHECKLIST.md, scripts/) — slide 17 #2."));

        // #3 — solicita/embebe credenciales.
        foreach (var c in Credenciales)
            if (lower.Contains(c, StringComparison.Ordinal))
            {
                hallazgos.Add(new(SeveridadSkill.Error, "credenciales",
                    $"Posible credencial en el skill (`{c}`). Las credenciales van en " +
                    "variables de entorno o Key Vault, NUNCA en el SKILL.md — slide 17 #3."));
                break;
            }

        // #5 — tools sin restringir. Lee la línea `allowed-tools:` del frontmatter.
        var tools = ExtraerAllowedTools(skillMd);
        foreach (var amplio in ToolsAmplios)
            if (tools.Any(t => string.Equals(t, amplio, StringComparison.OrdinalIgnoreCase)))
                hallazgos.Add(new(SeveridadSkill.Advertencia, "allowed-tools",
                    $"`allowed-tools` incluye `{amplio}`, demasiado amplio. Restringe al " +
                    "mínimo (`Bash(az *)`, `Read`) — menor privilegio, slide 17 #5."));

        return new InformeAntiPatrones(
            Limpio: hallazgos.Count == 0,
            Hallazgos: hallazgos);
    }

    private static IReadOnlyList<string> ExtraerAllowedTools(string skillMd)
    {
        foreach (var raw in skillMd.Replace("\r\n", "\n").Split('\n'))
        {
            var linea = raw.Trim();
            if (linea.StartsWith("allowed-tools:", StringComparison.OrdinalIgnoreCase))
            {
                var valor = linea["allowed-tools:".Length..].Trim().Trim('"', '\'');
                return valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }
        return [];
    }
}
