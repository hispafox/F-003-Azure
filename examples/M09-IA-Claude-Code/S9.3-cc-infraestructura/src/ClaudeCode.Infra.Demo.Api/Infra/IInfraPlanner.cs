namespace ClaudeCode.Infra.Demo.Api.Infra;

public sealed record PlanInfra(
    RequisitosInfra Requisitos,
    PromptInfra PromptBicep,
    PromptInfra PromptPipeline,
    InformeAudit? Audit,
    IReadOnlyList<string> Checklist);

// Compone InfraRequirementsParser + InfraPromptBuilder + InfraAuditChecker
// en el plan + checklist. Servicio inyectable (seam del test DI —
// lección M03-S3.4 / patrón M06-M09).
public interface IInfraPlanner
{
    PlanInfra Planificar(
        string descripcionRequisitos,
        IEnumerable<EstadoRecurso>? recursosExistentes = null);
}

public sealed class InfraPlanner : IInfraPlanner
{
    public PlanInfra Planificar(
        string descripcionRequisitos,
        IEnumerable<EstadoRecurso>? recursosExistentes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcionRequisitos);

        var req = InfraRequirementsParser.Parsear(descripcionRequisitos);
        var promptBicep = InfraPromptBuilder.ParaEscenario(
            EscenarioInfra.BicepDesdeRequirements, req);
        var promptPipeline = InfraPromptBuilder.ParaEscenario(
            EscenarioInfra.GhActionsPipeline);

        var audit = recursosExistentes is not null
            ? InfraAuditChecker.Auditar(recursosExistentes)
            : null;

        return new PlanInfra(
            Requisitos: req,
            PromptBicep: promptBicep,
            PromptPipeline: promptPipeline,
            Audit: audit,
            // Slides 2-17 transversal — checklist del flujo "requirements → IaC".
            Checklist:
            [
                "Parsea los requisitos (recursos + multi-region + GDPR + autoscale) — slide 2.",
                "Genera Bicep modular con AVM modules cuando aplique — slide 3/16.",
                "Verifica con `az bicep build` y `az deployment group what-if` — slide 8.",
                "Si hay recursos existentes, `az group export` + `az bicep decompile` y limpia con Claude — slide 16.",
                "Audita: HTTPS only, Managed Identity, tags, TLS 1.2, sin público — slide 15.",
                "Genera pipeline IaC con OIDC + what-if obligatorio en PRs — slide 17.",
                "Documenta cada decisión en un README.md generado por Claude — slide 17.",
                "Para incidentes recurrentes, genera un runbook con KQL + comandos `az` — slide 11.",
                "Para tareas operativas frecuentes, escribe un `ops-toolkit.sh` — slide 14.",
            ]);
    }
}
