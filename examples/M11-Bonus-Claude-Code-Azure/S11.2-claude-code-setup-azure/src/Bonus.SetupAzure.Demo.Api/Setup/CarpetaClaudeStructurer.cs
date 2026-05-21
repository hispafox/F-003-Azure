namespace Bonus.SetupAzure.Demo.Api.Setup;

public enum Prioridad { Obligatorio, Recomendado, Opcional }

public sealed record ItemEstructura(
    string Ruta,
    Prioridad Prioridad,
    string ParaQueSirve);

public sealed record EstructuraSugerida(
    IReadOnlyList<ItemEstructura> Items,
    IReadOnlyList<string> Avisos);

public sealed record EscenarioEquipo(
    bool TieneAgentsCustom = false,
    bool TieneSkillsPropios = false,
    bool TieneSlashCommandsCustom = false,
    bool QuiereHooks = false,
    bool UsaMcpServers = false,
    bool TrabajoIndividual = false);

// Slide 4 — estructurador del directorio `.claude/` para un proyecto
// Azure. Lógica pura. Devuelve qué archivos/carpetas son
// `Obligatorio` (todos los proyectos), `Recomendado` (la mayoría de
// equipos) u `Opcional` (según escenario).
public static class CarpetaClaudeStructurer
{
    public static EstructuraSugerida Inventariar(EscenarioEquipo e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var items = new List<ItemEstructura>
        {
            // Núcleo siempre.
            new("CLAUDE.md", Prioridad.Obligatorio,
                "Contexto del proyecto que Claude lee al arrancar (stack + " +
                "convenciones + zonas frágiles, slide 5)."),
            new(".claude/settings.json", Prioridad.Obligatorio,
                "Config compartida del equipo: model + permissions allow/deny + " +
                "hooks (slide 7)."),
            new(".claude/settings.local.json", Prioridad.Recomendado,
                "Overrides personales en `.gitignore` (slide 8) — modelo, " +
                "permisos extra que solo tú quieres."),
        };

        if (e.TieneAgentsCustom)
            items.Add(new(".claude/agents/", Prioridad.Recomendado,
                "Subagentes custom (`bicep-reviewer.md`, `security-auditor.md`, " +
                "`cost-analyzer.md`) — slide 4."));

        if (e.TieneSkillsPropios)
            items.Add(new(".claude/skills/", Prioridad.Recomendado,
                "Skills del proyecto: `our-conventions/SKILL.md`, " +
                "`deploy-checklist/SKILL.md` (slide 4)."));

        if (e.TieneSlashCommandsCustom)
            items.Add(new(".claude/commands/", Prioridad.Opcional,
                "Slash commands custom (`/daily-standup`, `/deploy-staging`) — slide 4/20."));

        if (e.QuiereHooks)
            items.Add(new(".claude/hooks/", Prioridad.Opcional,
                "Hooks pre/post comandos (slide 18). Logging + bloqueos por política."));

        if (e.UsaMcpServers)
            items.Add(new(".mcp.json", Prioridad.Recomendado,
                "MCP servers del proyecto: azure, bicep, azure-devops (slide 13/14). " +
                "Se commitea — equipo entero tiene las mismas herramientas."));

        var avisos = new List<string>();
        if (e.TrabajoIndividual && e.TieneAgentsCustom)
            avisos.Add("Trabajo individual: `.claude/agents/` se vuelve más útil al " +
                "compartir con un equipo; valora ponerlo en `~/.claude/agents/` global.");
        if (!e.UsaMcpServers && e.TieneSkillsPropios)
            avisos.Add("Los skills suelen funcionar mejor con MCP habilitado. Considera " +
                "añadir `.mcp.json` con al menos `filesystem` y `azure` (slide 14).");
        if (!e.QuiereHooks)
            avisos.Add("Sin hooks no hay defensa determinística contra acciones " +
                "destructivas; el `deny` de `settings.json` debe estar bien afinado (slide 18).");

        return new EstructuraSugerida(items, avisos);
    }
}
