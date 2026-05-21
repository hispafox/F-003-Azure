namespace Bonus.SkillsAzure.Demo.Api.Skills;

public enum SeveridadSkill { Error, Advertencia, Info }

public sealed record HallazgoSkill(
    SeveridadSkill Severidad, string Campo, string Mensaje);

// Frontmatter parseado del SKILL.md (slide 3/6).
public sealed record SkillFrontmatter(
    string? Name,
    string? Description,
    IReadOnlyList<string> AllowedTools,
    string? Context,
    string? Agent,
    string? Model);

public sealed record ValidacionFrontmatter(
    bool Valido,
    SkillFrontmatter Frontmatter,
    IReadOnlyList<HallazgoSkill> Hallazgos);

// Slide 6 — parser + validador del frontmatter del SKILL.md. Lógica
// pura. Parsea el bloque `---...---` (YAML simple, una clave por
// línea) y comprueba:
//   - falta `name` o `description` → Error (skill mal formado)
//   - `context: fork` sin `agent` → Advertencia (slide 14)
//   - sin `allowed-tools` → Advertencia (menor privilegio, slide 17)
public static class SkillFrontmatterValidator
{
    public static ValidacionFrontmatter Validar(string skillMd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillMd);

        var fm = Parsear(skillMd, out bool teniaBloque);
        var hallazgos = new List<HallazgoSkill>();

        if (!teniaBloque)
            hallazgos.Add(new(SeveridadSkill.Error, "frontmatter",
                "El SKILL.md no tiene bloque frontmatter `---...---` al inicio (slide 3)."));

        if (string.IsNullOrWhiteSpace(fm.Name))
            hallazgos.Add(new(SeveridadSkill.Error, "name",
                "Falta `name`. Es obligatorio: se usa para invocar el skill con `/name` (slide 6)."));

        if (string.IsNullOrWhiteSpace(fm.Description))
            hallazgos.Add(new(SeveridadSkill.Error, "description",
                "Falta `description`. Sin ella Claude nunca carga el skill (slide 6)."));

        if (string.Equals(fm.Context, "fork", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(fm.Agent))
            hallazgos.Add(new(SeveridadSkill.Advertencia, "agent",
                "`context: fork` sin `agent`. Declara qué agent usa el subagent " +
                "(ej. `agent: Explore`) — slide 14."));

        if (fm.AllowedTools.Count == 0)
            hallazgos.Add(new(SeveridadSkill.Advertencia, "allowed-tools",
                "Sin `allowed-tools`: el skill hereda todos los tools. Aplica menor " +
                "privilegio — declara solo los que necesita (slide 17)."));

        bool valido = !hallazgos.Any(h => h.Severidad == SeveridadSkill.Error);
        return new ValidacionFrontmatter(valido, fm, hallazgos);
    }

    // Parser de frontmatter YAML simple: bloque entre la primera y la
    // segunda línea `---`. Una clave por línea (`clave: valor`).
    private static SkillFrontmatter Parsear(string skillMd, out bool teniaBloque)
    {
        teniaBloque = false;
        var campos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var lineas = skillMd.Replace("\r\n", "\n").Split('\n');
        int i = 0;
        while (i < lineas.Length && lineas[i].Trim().Length == 0) i++;

        if (i >= lineas.Length || lineas[i].Trim() != "---")
            return Vacio();

        teniaBloque = true;
        for (i++; i < lineas.Length; i++)
        {
            var linea = lineas[i].Trim();
            if (linea == "---") break;
            if (linea.Length == 0 || linea.StartsWith('#')) continue;

            int sep = linea.IndexOf(':');
            if (sep <= 0) continue;

            var clave = linea[..sep].Trim();
            var valor = linea[(sep + 1)..].Trim().Trim('"', '\'');
            // Quita comentario inline (`agent: Explore   # qué agent`).
            int comentario = valor.IndexOf('#');
            if (comentario >= 0) valor = valor[..comentario].Trim();
            campos[clave] = valor;
        }

        return new SkillFrontmatter(
            Name: Get(campos, "name"),
            Description: Get(campos, "description"),
            AllowedTools: ParseTools(Get(campos, "allowed-tools")),
            Context: Get(campos, "context"),
            Agent: Get(campos, "agent"),
            Model: Get(campos, "model"));

        static SkillFrontmatter Vacio() =>
            new(null, null, [], null, null, null);
    }

    private static string? Get(Dictionary<string, string> campos, string clave) =>
        campos.TryGetValue(clave, out var v) && v.Length > 0 ? v : null;

    private static IReadOnlyList<string> ParseTools(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return [];
        return valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
