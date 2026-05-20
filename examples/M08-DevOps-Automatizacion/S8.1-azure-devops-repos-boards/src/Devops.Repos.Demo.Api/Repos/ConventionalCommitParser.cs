using System.Text.RegularExpressions;

namespace Devops.Repos.Demo.Api.Repos;

// Slide 7 — tipos canónicos de Conventional Commits.
public static class TiposCommit
{
    public static readonly HashSet<string> Validos = new(StringComparer.Ordinal)
    {
        "feat", "fix", "docs", "refactor", "test",
        "chore", "perf", "ci", "build", "style",
    };
}

public sealed record CommitParseado(
    bool Valido,
    string Tipo,
    string? Scope,
    bool BreakingChange,
    string Descripcion,
    IReadOnlyList<int> WorkItems,
    IReadOnlyList<string> Problemas);

// Slide 7 + 12 — parser de Conventional Commits con vínculo a work
// items por `#NNNN` o `Fixes #NNNN` (slide 12). Lógica pura.
public static partial class ConventionalCommitParser
{
    // tipo(scope)!: descripción
    [GeneratedRegex(@"^(?<tipo>[a-z]+)(?:\((?<scope>[^)]+)\))?(?<break>!)?:\s*(?<desc>.+)$")]
    private static partial Regex Encabezado();

    [GeneratedRegex(@"#(?<id>\d+)")]
    private static partial Regex WorkItemRef();

    public static CommitParseado Parsear(string mensaje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);

        // El "encabezado" es la primera línea (slide 7).
        string encabezado = mensaje.Split('\n', 2)[0].Trim();
        var m = Encabezado().Match(encabezado);
        var problemas = new List<string>();

        if (!m.Success)
            return new CommitParseado(false, "", null, false, encabezado, [],
                ["Formato inválido. Esperado: tipo(scope)?: descripción (slide 7)."]);

        string tipo = m.Groups["tipo"].Value;
        if (!TiposCommit.Validos.Contains(tipo))
            problemas.Add($"Tipo '{tipo}' no es uno de: {string.Join(", ", TiposCommit.Validos)} (slide 7).");

        string descripcion = m.Groups["desc"].Value.Trim();
        if (descripcion.Length == 0)
            problemas.Add("Descripción vacía (slide 7).");

        // Slide 12 — work items se referencian con #NNNN en cualquier
        // parte del mensaje (encabezado o cuerpo).
        var workItems = WorkItemRef()
            .Matches(mensaje)
            .Select(x => int.Parse(x.Groups["id"].Value))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return new CommitParseado(
            Valido: problemas.Count == 0,
            Tipo: tipo,
            Scope: m.Groups["scope"].Success ? m.Groups["scope"].Value : null,
            BreakingChange: m.Groups["break"].Success,
            Descripcion: descripcion,
            WorkItems: workItems,
            Problemas: problemas);
    }
}
