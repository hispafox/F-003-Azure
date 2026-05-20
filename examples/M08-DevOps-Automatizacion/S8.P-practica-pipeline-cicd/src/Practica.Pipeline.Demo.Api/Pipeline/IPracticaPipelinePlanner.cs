namespace Practica.Pipeline.Demo.Api.Pipeline;

public sealed record PlanPracticaPipeline(
    ReportePreflight Preflight,
    EsqueletoPipeline Pipeline,
    ResultadoSmoke? SmokeTest,
    IReadOnlyList<string> Checklist);

// Compone PreflightChecker + PipelineStageBuilder + SmokeTestEvaluator
// en el plan + checklist de la práctica. Servicio inyectable (seam del
// test DI — lección M03-S3.4 / patrón M06-M08).
public interface IPracticaPipelinePlanner
{
    PlanPracticaPipeline Planificar(
        EscenarioPreflight preflight,
        OpcionesPipeline opciones,
        MedidasSmoke? simulacionSmoke = null,
        UmbralesSmoke? umbrales = null);
}

public sealed class PracticaPipelinePlanner : IPracticaPipelinePlanner
{
    public PlanPracticaPipeline Planificar(
        EscenarioPreflight preflight,
        OpcionesPipeline opciones,
        MedidasSmoke? simulacionSmoke = null,
        UmbralesSmoke? umbrales = null)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(opciones);

        var pf = PreflightChecker.Comprobar(preflight);
        var pipeline = PipelineStageBuilder.Construir(opciones);
        var smoke = simulacionSmoke is not null
            ? SmokeTestEvaluator.Evaluar(simulacionSmoke, umbrales)
            : null;

        return new PlanPracticaPipeline(
            Preflight: pf,
            Pipeline: pipeline,
            SmokeTest: smoke,
            // Slide 11 — checklist canónica de la práctica.
            Checklist:
            [
                "azure-pipelines.yml creado en la raíz del repo (slide 4)",
                "Pipeline creado en Azure DevOps Portal (slide 7)",
                "Service Connection con OIDC (Workload Identity, slide 3/17)",
                "Environment 'staging' SIN aprobación (slide 5/7)",
                "Environment 'production' CON aprobación + reviewers (slide 6/7)",
                "Push a main dispara el pipeline (slide 8)",
                "Build + Test stage verde (slide 4)",
                "Deploy Staging + smoke test contra slot (slide 5)",
                "Aprobación manual solicitada antes del swap (slide 7)",
                "Swap a producción ejecutado (slide 6)",
                "Health check post-swap verifica /health == 200 (slide 6)",
                "Auto-rollback configurado si validation falla (slide 10)",
            ]);
    }
}
