using Practica.Pipeline.Demo.Api.Pipeline;

namespace Practica.Pipeline.Demo.Api.Endpoints;

public sealed record SmokeRequest(MedidasSmoke Medidas, UmbralesSmoke? Umbrales = null);

public sealed record PlanRequest(
    EscenarioPreflight Preflight,
    OpcionesPipeline Opciones,
    MedidasSmoke? SimulacionSmoke = null,
    UmbralesSmoke? Umbrales = null);

public static class PipelineEndpoints
{
    public static void MapPipeline(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/pipeline");

        // Slide 3 — comprobaciones pre-flight de la práctica.
        g.MapPost("/preflight", (EscenarioPreflight e) =>
            Results.Ok(PreflightChecker.Comprobar(e)));

        // Slides 4-6/10/17 — esqueleto del pipeline canónico.
        g.MapPost("/etapas", (OpcionesPipeline o) =>
            Results.Ok(PipelineStageBuilder.Construir(o)));

        // Slide 5/6/10 — evaluador del smoke test post-deploy.
        g.MapPost("/smoke", (SmokeRequest r) =>
            Results.Ok(SmokeTestEvaluator.Evaluar(r.Medidas, r.Umbrales)));

        // Slide 11 — plan + checklist de la práctica.
        g.MapPost("/plan", (PlanRequest req, IPracticaPipelinePlanner planner) =>
            Results.Ok(planner.Planificar(
                req.Preflight, req.Opciones, req.SimulacionSmoke, req.Umbrales)));
    }
}
