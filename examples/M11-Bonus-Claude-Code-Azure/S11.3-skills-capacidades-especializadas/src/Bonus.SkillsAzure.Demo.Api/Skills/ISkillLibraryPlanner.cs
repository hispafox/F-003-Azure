namespace Bonus.SkillsAzure.Demo.Api.Skills;

public sealed record PlanSkill(
    ValidacionFrontmatter? Frontmatter,
    EvaluacionDescription? Description,
    InformeAntiPatrones? AntiPatrones,
    IReadOnlyList<string> SkillsMicrosoft,
    IReadOnlyList<string> SkillsRecomendadosEquipo,
    IReadOnlyList<FaseRoadmap> Roadmap,
    IReadOnlyList<string> Checklist);

public sealed record FaseRoadmap(string Cuando, IReadOnlyList<string> Skills);

public sealed record PlanRequest(string? SkillMd = null);

// Compone SkillFrontmatterValidator + SkillDescriptionScorer +
// SkillAntiPatternDetector + listas canónicas (los skills oficiales de
// Microsoft del slide 18, los skills recomendados del equipo de los
// slides 9-13 y el roadmap del slide 27). Servicio inyectable.
public interface ISkillLibraryPlanner
{
    PlanSkill Planificar(PlanRequest req);
}

public sealed class SkillLibraryPlanner : ISkillLibraryPlanner
{
    // Slide 18 — los 8 skills de Microsoft más usados en el día a día.
    public static IReadOnlyList<string> SkillsMicrosoftSlide18 { get; } =
    [
        "azure-prepare — prepara la app para Azure (detecta stack, sugiere servicio).",
        "azure-validate — valida azure.yaml + bicep + parameters antes de `azd up`.",
        "azure-deploy — ejecuta `azd up` / `az deployment` con los flags correctos.",
        "azure-diagnostics — troubleshooting de logs, eventos, health probes.",
        "azure-observability — configura App Insights, dashboards y alertas.",
        "azure-rbac — sugiere el rol mínimo + genera `az role assignment create`.",
        "azure-cost-optimization — auditoría de costes con recomendaciones accionables.",
        "azure-storage — operaciones seguras con Storage (soft delete, versioning).",
    ];

    // Slides 9-13 — los skills propios que todo equipo Azure debería tener.
    public static IReadOnlyList<string> SkillsRecomendadosEquipo { get; } =
    [
        "convenciones-equipo — aplica las convenciones .NET + Azure al generar/revisar (slide 9).",
        "bicep-modular-pattern — Bicep modular en infrastructure/modules/ (slide 10).",
        "azure-cost-review — auditoría de coste sobre un resource group (slide 11).",
        "security-review-azure — auditoría de seguridad infra + código (slide 12).",
        "deploy-checklist-prod — checklist pre-deploy a producción (slide 13).",
    ];

    // Slide 27 — roadmap típico de adopción de skills.
    public static IReadOnlyList<FaseRoadmap> RoadmapSlide27 { get; } =
    [
        new("Semana 1-2 · básicos", ["convenciones-equipo", "deploy-checklist-staging"]),
        new("Mes 1-2 · operativos",
            ["deploy-to-prod", "rollback-emergencia", "investigar-incidente",
             "generar-informe-semanal"]),
        new("Mes 3-6 · dominio",
            ["migrate-clickonce-msix", "generate-azure-function", "review-bicep-module",
             "create-feature-branch"]),
        new("Mes 6+ · avanzados",
            ["architecture-decision (context fork)", "cost-optimization-deep-dive",
             "security-audit-enterprise"]),
    ];

    public PlanSkill Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        ValidacionFrontmatter? frontmatter = null;
        EvaluacionDescription? description = null;
        InformeAntiPatrones? antiPatrones = null;

        if (!string.IsNullOrWhiteSpace(req.SkillMd))
        {
            frontmatter = SkillFrontmatterValidator.Validar(req.SkillMd);
            antiPatrones = SkillAntiPatternDetector.Detectar(req.SkillMd);
            if (!string.IsNullOrWhiteSpace(frontmatter.Frontmatter.Description))
                description = SkillDescriptionScorer.Evaluar(frontmatter.Frontmatter.Description!);
        }

        return new PlanSkill(
            Frontmatter: frontmatter,
            Description: description,
            AntiPatrones: antiPatrones,
            SkillsMicrosoft: SkillsMicrosoftSlide18,
            SkillsRecomendadosEquipo: SkillsRecomendadosEquipo,
            Roadmap: RoadmapSlide27,
            // Slides 2/15/22 — checklist de adopción de skills.
            Checklist:
            [
                "Crea el skill en `.claude/skills/<name>/SKILL.md` (proyecto) o `~/.claude/skills/` (personal) — slide 4.",
                "Usa `/skill-creator` para generar el frontmatter + template (slide 15).",
                "Escribe una `description` específica con keywords: Claude la usa para cargarlo (slide 16).",
                "Aplica menor privilegio en `allowed-tools` (solo lo necesario) — slide 17 #5.",
                "Saca lo procedural al skill; lo que aplica a todo el proyecto va en CLAUDE.md (slide 17 #1).",
                "Si crece, usa archivos de apoyo (CHECKLIST.md, scripts/, templates/) — slide 8.",
                "Versiona los skills del proyecto en Git y revísalos por PR (slide 22/23).",
                "Instala el plugin oficial: `/plugin install azure-skills@microsoft-azure` (slide 18).",
            ]);
    }
}
