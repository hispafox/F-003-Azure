namespace Pipelines.Demo.Api.Pipelines;

public sealed record PlanPipeline(
    PipelineDef Estructura,
    ResultadoValidacion Validacion,
    IReadOnlyList<RecomendacionTrigger> TriggersEstandar,
    IReadOnlyList<string> Checklist);

// Compone PipelineYamlParser + PipelineStructureValidator +
// TriggerAdvisor en el plan + checklist del entregable. Servicio
// inyectable (seam del test DI — lección M03-S3.4).
public interface IPipelinePlanner
{
    PlanPipeline PlanificarDesdeYaml(string yaml);
}

public sealed class PipelinePlanner : IPipelinePlanner
{
    public PlanPipeline PlanificarDesdeYaml(string yaml)
    {
        var pipeline = PipelineYamlParser.Parsear(yaml);
        var val = PipelineStructureValidator.Validar(pipeline);
        return new PlanPipeline(
            pipeline,
            val,
            TriggerAdvisor.RecomendacionEstandar(),
            // Slides 4, 6, 7, 8, 9, 15, 16, 22 — checklist completo.
            Checklist:
            [
                "Pipeline as Code: YAML versionado en el repo (slide 2)",
                "Triggers: branch main + PR validation + nightly opcional (slide 4)",
                "Stage Build con dotnet build + dotnet test + Publish results (slide 6)",
                "Code coverage publicado (PublishCodeCoverageResults@2, slide 6)",
                "CD: deploy a slot staging → swap a producción (slide 7)",
                "Environments con approval para producción (slide 8)",
                "Variable Groups linked a Key Vault para secretos (slide 9)",
                "Service Connection a Azure con OIDC/Federated Identity (slide 15/22)",
                "Cache de NuGet y npm para acelerar builds (slide 16)",
                "Notificaciones de fallo en Teams/Slack (slide 20)",
            ]);
    }
}
