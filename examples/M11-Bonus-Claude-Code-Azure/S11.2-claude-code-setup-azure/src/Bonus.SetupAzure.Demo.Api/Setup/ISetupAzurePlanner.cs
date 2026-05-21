namespace Bonus.SetupAzure.Demo.Api.Setup;

public sealed record PlanSetup(
    EstructuraSugerida Estructura,
    InformeSettings? Settings,
    EvaluacionClaudeMd? ClaudeMd,
    IReadOnlyList<string> AzureSkillsDisponibles,
    IReadOnlyList<string> Checklist);

public sealed record PlanRequest(
    EscenarioEquipo Equipo,
    EscenarioSettings? Settings = null,
    string? ClaudeMdContenido = null);

// Compone CarpetaClaudeStructurer + SettingsPermissionsValidator +
// ClaudeMdQualityEvaluator + lista canónica de los 20 skills del
// Azure Skills Plugin (slide 16). Servicio inyectable.
public interface ISetupAzurePlanner
{
    PlanSetup Planificar(PlanRequest req);
}

public sealed class SetupAzurePlanner : ISetupAzurePlanner
{
    // Slide 16 — los 20 skills del Azure Skills Plugin oficial.
    public static IReadOnlyList<string> AzureSkillsSlide16 { get; } =
    [
        // Build & Deploy
        "azure-prepare — prepara una app para Azure (elige servicio).",
        "azure-validate — valida archivos de despliegue.",
        "azure-deploy — ejecuta el deploy a Azure.",
        // Troubleshoot & Operate
        "azure-diagnostics — diagnostica problemas de recursos.",
        "azure-observability — logs, métricas, App Insights.",
        "azure-compliance — verificación de compliance.",
        // Optimize & Design
        "azure-cost-optimization — análisis y recomendaciones de coste.",
        "azure-compute — elegir servicio de cómputo.",
        "azure-resource-visualizer — diagramas de recursos.",
        // Data, AI & Platform
        "azure-ai — trabajo con Azure AI Services.",
        "azure-aigateway — AI Gateway configuration.",
        "azure-storage — operaciones de Storage Account.",
        "azure-kusto — Kusto Queries (KQL).",
        "azure-rbac — RBAC (roles, asignaciones).",
        "azure-cloud-migrate — migraciones on-prem → Azure.",
        "entra-app-registration — App registrations en Entra ID.",
        "microsoft-foundry — modelos AI en Foundry.",
        // MCP servers incluidos
        "azure-mcp (server) — recursos Azure live.",
        "foundry-mcp (server) — modelos de Foundry.",
        "azure-bicep (server) — schema/lint Bicep en VS Code y CC.",
    ];

    public PlanSetup Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var estructura = CarpetaClaudeStructurer.Inventariar(req.Equipo);
        var settings = req.Settings is not null
            ? SettingsPermissionsValidator.Validar(req.Settings)
            : null;
        var claudeMd = !string.IsNullOrWhiteSpace(req.ClaudeMdContenido)
            ? ClaudeMdQualityEvaluator.Evaluar(req.ClaudeMdContenido)
            : null;

        return new PlanSetup(
            Estructura: estructura,
            Settings: settings,
            ClaudeMd: claudeMd,
            AzureSkillsDisponibles: AzureSkillsSlide16,
            // Slides 2/3/15 — checklist de arranque.
            Checklist:
            [
                "`npm install -g @anthropic-ai/claude-code` + `claude --version` (slide 2).",
                "Autentica: OAuth con `claude auth login` o `ANTHROPIC_API_KEY` (slide 3).",
                "Crea `CLAUDE.md` con Stack + Convenciones + ZonasFragiles (slide 5).",
                "Crea `.claude/settings.json` con `allow` + `deny` afinados (slide 7).",
                "Añade `.gitignore` de `.claude/settings.local.json` (slide 8).",
                "Instala Azure Skills Plugin: `/plugin install azure-skills@microsoft-azure` (slide 15).",
                "Crea `.mcp.json` con `azure`, `bicep`, `azure-devops` (slide 14).",
                "Activa el hook `PreToolUse` para logging y políticas (slide 18).",
                "Prueba con `> Analiza este proyecto. Dime el stack y los riesgos` (slide 11).",
            ]);
    }
}
