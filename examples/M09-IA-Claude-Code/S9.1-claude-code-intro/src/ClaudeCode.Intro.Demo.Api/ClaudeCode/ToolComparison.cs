namespace ClaudeCode.Intro.Demo.Api.ClaudeCode;

public enum HerramientaIa { ClaudeCode, GithubCopilot, Combinacion }

public sealed record FilaComparativa(
    string Aspecto, string ClaudeCode, string GithubCopilot);

public sealed record EscenarioElegirHerramienta(
    bool QuieresAutocompletadoEnIde = true,
    bool NecesitasAgenteQueEjecuta = false,
    bool ProyectoMultiArchivo = false,
    bool NecesitasMcp = false,
    bool TienesPresupuestoFijo = true);

public sealed record RecomendacionHerramienta(
    HerramientaIa Herramienta, IReadOnlyList<string> Razones);

// Slide 5 — Claude Code vs GitHub Copilot. Lógica pura: tabla canónica
// y heurística por escenario. La conclusión típica es "ambas a la vez".
public static class ToolComparison
{
    public static IReadOnlyList<FilaComparativa> Tabla { get; } =
    [
        new("Modelo", "Claude (Anthropic)", "GPT/Codex (OpenAI)"),
        new("Interfaz", "Terminal (CLI)", "IDE (VS Code, JetBrains)"),
        new("Contexto", "Proyecto completo + MCP", "Archivo actual + vecinos"),
        new("Agente (ejecuta)", "Sí (Bash, Edit, Write)", "Parcial (Copilot Workspace)"),
        new("MCP", "Sí", "No"),
        new("Multi-archivo", "Sí (N archivos por turn)", "Limitado"),
        new("Tipo de coste", "API usage", "Suscripción fija"),
        new("Mejor para", "Tareas largas, IaC, refactor", "Autocompletado mientras tecleas"),
    ];

    public static RecomendacionHerramienta Recomendar(EscenarioElegirHerramienta e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Si el alumno marca señales fuertes de ambas, no son
        // excluyentes (slide 5).
        bool senalesClaudeCode = e.NecesitasAgenteQueEjecuta
            || e.NecesitasMcp
            || e.ProyectoMultiArchivo;
        bool senalesCopilot = e.QuieresAutocompletadoEnIde;

        if (senalesClaudeCode && senalesCopilot)
            return new RecomendacionHerramienta(
                Herramienta: HerramientaIa.Combinacion,
                Razones:
                [
                    "Copilot para autocompletado mientras tecleas (slide 5).",
                    "Claude Code para tareas grandes (generar módulos, IaC, debugging).",
                    "No son excluyentes; el coste suma pero se compensa con " +
                        "productividad.",
                ]);

        if (senalesClaudeCode)
            return new RecomendacionHerramienta(
                Herramienta: HerramientaIa.ClaudeCode,
                Razones:
                [
                    e.NecesitasAgenteQueEjecuta
                        ? "Necesitas ejecutar comandos (build, test, deploy) → Claude Code."
                        : "Necesitas contexto multi-archivo o MCP → Claude Code.",
                    "Coste variable por uso (API); ajustas según volumen.",
                ]);

        // Solo IDE / autocompletado / presupuesto fijo → Copilot solo.
        return new RecomendacionHerramienta(
            Herramienta: HerramientaIa.GithubCopilot,
            Razones:
            [
                "Solo necesitas autocompletado en el IDE → Copilot suficiente (slide 5).",
                e.TienesPresupuestoFijo
                    ? "Presupuesto fijo: suscripción mensual predecible."
                    : "Sin requisitos de agente ni MCP.",
            ]);
    }
}
