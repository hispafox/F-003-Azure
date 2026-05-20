namespace ClaudeCode.Intro.Demo.Api.ClaudeCode;

public sealed record SettingsRecomendados(
    string Model,
    int MaxTokens,
    string SystemPrompt,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> ExcludePatterns,
    IReadOnlyList<string> HooksRecomendados);

public sealed record EscenarioEquipo(
    string LenguajePrincipal = "csharp",
    string Framework = "net10.0",
    bool CursoEnProduccion = false,
    bool RequiereCompliance = false,
    bool TocaInfraestructura = true);

// Slides 6, 11, 13, 19 — builder del `.claude/settings.json` recomendado
// para un equipo. Devuelve la forma canónica (model, allowedTools,
// excludePatterns, hooks) sin emitir JSON literal — el alumno escribe
// el JSON en clase. Lógica pura.
public static class ProjectConfigBuilder
{
    // Slide 11 — patrones que NUNCA debe ver Claude Code.
    public static IReadOnlyList<string> ExcludePatternsBase { get; } =
    [
        "*.env",
        ".secrets/*",
        "*.pfx",
        "*.key",
        "*.pem",
        "local.settings.json",
        "appsettings.*.local.json",
    ];

    public static SettingsRecomendados Construir(EscenarioEquipo e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Slide 13 — allowed tools mínimas para empezar.
        var allowed = new List<string> { "Read", "Glob", "Grep", "Edit", "Write" };
        if (e.TocaInfraestructura) allowed.Add("Bash");

        // Slide 19 — hooks recomendados según el riesgo.
        var hooks = new List<string>
        {
            "PreToolUse(Bash) → scripts/block-dangerous.sh (slide 19)",
            "PostToolUse(Write|Edit) → scripts/auto-format.sh (slide 19)",
        };
        if (e.CursoEnProduccion || e.RequiereCompliance)
            hooks.Add("PreToolUse(Write|Edit) → scripts/block-secrets.sh " +
                "(detecta password/api_key/token, slide 19)");
        if (e.CursoEnProduccion)
            hooks.Add("PreToolUse(Bash → git commit) → scripts/pre-commit-validation.sh " +
                "(build + test + secrets, slide 19)");

        // Slide 6/13 — system prompt mínimo del equipo.
        var systemPrompt =
            $"Eres un desarrollador senior trabajando con {e.LenguajePrincipal} " +
            $"({e.Framework}). Usa async/await siempre, ILogger para logging, " +
            "records para DTOs, y Managed Identity para conexiones cuando aplique. " +
            "Tests con xUnit + WebApplicationFactory. " +
            "Nombres en inglés, comentarios en español.";

        return new SettingsRecomendados(
            // Slide 13 — modelo por defecto sugerido (sonnet equilibra
            // calidad y coste; opus para tareas muy complejas).
            Model: "claude-sonnet-4-6",
            MaxTokens: 8192,
            SystemPrompt: systemPrompt,
            AllowedTools: allowed,
            ExcludePatterns: ExcludePatternsBase,
            HooksRecomendados: hooks);
    }
}
