namespace ClaudeCode.Intro.Demo.Api.ClaudeCode;

public sealed record PlanClaudeCode(
    RecomendacionHerramienta Herramienta,
    RecomendacionFeature? Feature,
    SettingsRecomendados Settings,
    IReadOnlyList<string> Checklist);

// Compone ToolComparison + FeatureRecommender + ProjectConfigBuilder en
// el plan + checklist del onboarding a Claude Code. Servicio inyectable
// (seam del test DI — lección M03-S3.4 / patrón M06-M08).
public interface IClaudeCodePlanner
{
    PlanClaudeCode Planificar(
        EscenarioElegirHerramienta herramienta,
        EscenarioEquipo equipo,
        EscenarioTarea? tareaConcreta = null);
}

public sealed class ClaudeCodePlanner : IClaudeCodePlanner
{
    public PlanClaudeCode Planificar(
        EscenarioElegirHerramienta herramienta,
        EscenarioEquipo equipo,
        EscenarioTarea? tareaConcreta = null)
    {
        ArgumentNullException.ThrowIfNull(herramienta);
        ArgumentNullException.ThrowIfNull(equipo);

        return new PlanClaudeCode(
            Herramienta: ToolComparison.Recomendar(herramienta),
            Feature: tareaConcreta is not null
                ? FeatureRecommender.Recomendar(tareaConcreta)
                : null,
            Settings: ProjectConfigBuilder.Construir(equipo),
            // Slides 3, 6, 11, 13, 18, 19, 20 — checklist de onboarding.
            Checklist:
            [
                "Node.js 18+ instalado y `claude --version` responde (slide 3)",
                "API key configurada con `claude auth login` o `ANTHROPIC_API_KEY` (slide 3)",
                "`.claude/settings.json` versionado con allowed tools + exclude patterns (slide 13)",
                "`.claude/config.yml` o system prompt con las convenciones del equipo (slide 6)",
                "Excluir `*.env`, `*.pfx`, `local.settings.json`, `.secrets/*` (slide 11)",
                "Hook `PreToolUse(Bash)` para bloquear comandos destructivos (slide 19)",
                "Hook `PostToolUse(Write|Edit)` para auto-format (slide 19)",
                "Subagent `code-reviewer` en `.claude/agents/` para PRs (slide 18)",
                "Skill `deploy-staging` o equivalente en `.claude/skills/` (slide 20)",
                "Pipeline step `claude -p ... --no-interactive` para AI code review (slide 16)",
            ]);
    }
}
