namespace Plataforma.Demo.Api.Plataforma;

public sealed record PlanPlataforma(
    RecomendacionPlataforma Recomendacion,
    ComparativaCoste Coste,
    IReadOnlyList<Equivalencia> EquivalenciasClave,
    IReadOnlyList<string> Checklist);

// Compone PlatformAdvisor + MigrationCostEstimator +
// SyntaxEquivalenceMapper en el plan + checklist. Servicio inyectable
// (seam del test DI — lección M03-S3.4).
public interface IPlatformPlanner
{
    PlanPlataforma Planificar(
        EscenarioPlataforma escenario, EscenarioCoste coste);
}

public sealed class PlatformPlanner : IPlatformPlanner
{
    // Equivalencias más usadas (slide 6) — el resto vive en
    // SyntaxEquivalenceMapper.Todas y se sirve por su propio endpoint.
    private static readonly string[] ConceptosClave =
    [
        "Jerarquía", "Trigger en main", "Pool / runner",
        "Setup .NET", "Deploy App Service",
        "Login Azure", "Secreto", "Job depende de otro",
    ];

    public PlanPlataforma Planificar(
        EscenarioPlataforma escenario, EscenarioCoste coste)
    {
        ArgumentNullException.ThrowIfNull(escenario);
        ArgumentNullException.ThrowIfNull(coste);

        var equivalenciasClave = ConceptosClave
            .Select(SyntaxEquivalenceMapper.Buscar)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        return new PlanPlataforma(
            PlatformAdvisor.Recomendar(escenario),
            MigrationCostEstimator.Comparar(coste),
            equivalenciasClave,
            // Slides 4, 5, 7, 8, 11, 12, 13, 19, 20 — checklist.
            Checklist:
            [
                "Si ya usas ADO y funciona: mantén ADO (slide 4/7) — no migres sin beneficio claro",
                "Equipos 6-10 personas con sprints: ADO Boards > GitHub Projects (slide 11)",
                "Open source / Copilot / Dependabot: GitHub (slide 5/9/10)",
                "Híbrido viable: repos en GitHub + Boards/Pipelines en ADO (slide 8)",
                "GitHub Advanced Security disponible también en ADO desde 2023 (slide 13)",
                "Misma lógica YAML, sintaxis distinta: stages/jobs/steps ↔ jobs/steps (slide 6)",
                "$(var) → ${{ var }}; task → uses; dependsOn → needs (slide 6)",
                "Coste para equipos pequeños con Azure: ADO suele ser más barato (slide 12)",
                "Migración real: repos directos (git push), work items con herramientas, pipelines manual (slide 7)",
                "Lessons learned: no migrar 'por modernizar' sin un beneficio claro y medible (slide 20)",
            ]);
    }
}
