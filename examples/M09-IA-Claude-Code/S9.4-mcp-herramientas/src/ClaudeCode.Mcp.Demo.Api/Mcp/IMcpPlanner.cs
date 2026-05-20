namespace ClaudeCode.Mcp.Demo.Api.Mcp;

public sealed record PlanMcp(
    IReadOnlyList<ServerSugerido> ServersRecomendados,
    McpConfig? ConfigActual,
    InformeSeguridad? Seguridad,
    IReadOnlyList<string> Checklist);

// Compone McpConfigParser + McpServerRecommender + McpSecurityChecker
// en el plan + checklist. Servicio inyectable (seam del test DI —
// lección M03-S3.4 / patrón M06-M09).
public interface IMcpPlanner
{
    PlanMcp Planificar(EscenarioMcp escenario, string? configJson = null);
}

public sealed class McpPlanner : IMcpPlanner
{
    public PlanMcp Planificar(EscenarioMcp escenario, string? configJson = null)
    {
        ArgumentNullException.ThrowIfNull(escenario);

        var recomendados = McpServerRecommender.Recomendar(escenario);
        var config = !string.IsNullOrWhiteSpace(configJson)
            ? McpConfigParser.Parsear(configJson)
            : null;
        var seguridad = config is not null
            ? McpSecurityChecker.Comprobar(config)
            : null;

        return new PlanMcp(
            ServersRecomendados: recomendados,
            ConfigActual: config,
            Seguridad: seguridad,
            // Slides 3, 9, 10, 11 — checklist del onboarding MCP.
            Checklist:
            [
                "Identifica las herramientas externas que tu equipo usa (ADO/GitHub/DB/Notion).",
                "Habilita el `filesystem` MCP server restringido a la carpeta del proyecto (slide 3).",
                "Crea tokens con permisos mínimos: read-only por defecto, write SOLO si Claude crea PRs/issues (slide 9).",
                "Pon las credenciales como `${VAR}` o `$env:VAR` — nunca en plano en el config (slide 9).",
                "Versiona el `claude_desktop_config.json` SIN las credenciales (template + variables).",
                "Documenta el calendario de rotación de tokens (90 días por defecto).",
                "Prueba el flujo end-to-end del slide 10 con un work item / bug de prueba.",
                "Para flujos recurrentes (daily briefing), considera scripts headless (`claude -p`, slide 14).",
            ]);
    }
}
