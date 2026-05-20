using Pipelines.Demo.Api.Pipelines;

namespace Pipelines.Demo.Api.Endpoints;

public sealed record YamlRequest(string Yaml);

public static class PipelinesEndpoints
{
    public static void MapPipelines(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var p = app.MapGroup("/pipeline");

        // Slide 3 — parsea el azure-pipelines.yml a la estructura interna.
        p.MapPost("/parsear", (YamlRequest r) =>
            Results.Ok(PipelineYamlParser.Parsear(r.Yaml)));

        // Slides 3, 5, 6, 7, 8 — valida la estructura (errores + avisos).
        p.MapPost("/validar", (YamlRequest r) =>
            Results.Ok(PipelineStructureValidator.Validar(
                PipelineYamlParser.Parsear(r.Yaml))));

        // Slide 4 — bloque YAML recomendado por escenario.
        p.MapGet("/trigger/recomendado", (EscenarioTrigger escenario) =>
            Results.Ok(TriggerAdvisor.Recomendar(escenario)));

        // Slide 4 — recomendación estándar (CI + PR + nightly).
        p.MapGet("/trigger/estandar", () =>
            Results.Ok(TriggerAdvisor.RecomendacionEstandar()));

        // Plan + checklist del entregable.
        p.MapPost("/plan", (YamlRequest r, IPipelinePlanner planner) =>
            Results.Ok(planner.PlanificarDesdeYaml(r.Yaml)));
    }
}
