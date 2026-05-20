using Deploy.Demo.Api.Deploy;

namespace Deploy.Demo.Api.Endpoints;

public sealed record HealthRequest(
    int StatusEsperado, int MaxIntentos, List<HealthAttempt> Intentos);

public sealed record SmokeRequestBody(List<SmokeRequest> Requests);

public static class DeployEndpoints
{
    public static void MapDeploy(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var d = app.MapGroup("/deploy");

        // Slide 3 — estrategia recomendada por tipo de app.
        d.MapGet("/estrategia",
            (TipoApp tipoApp, bool? tieneSlots, bool? planPremium, bool? critico) =>
                Results.Ok(DeployStrategyAdvisor.Recomendar(
                    new EscenarioDeploy(tipoApp,
                        tieneSlots ?? false, planPremium ?? false, critico ?? false))));

        // Slide 9 — evaluación del health check post-deploy.
        d.MapPost("/healthcheck", (HealthRequest r) =>
            Results.Ok(HealthCheckEvaluator.Evaluar(
                r.StatusEsperado, r.MaxIntentos, r.Intentos)));

        // Slide 9 — smoke test funcional (varios endpoints).
        d.MapPost("/smoke", (SmokeRequestBody b) =>
            Results.Ok(HealthCheckEvaluator.EvaluarSmoke(b.Requests)));

        // Slide 8 — plan de rollback por tipo de app.
        d.MapGet("/rollback",
            (TipoApp tipoApp, bool? tieneSlots, bool? planPremium) =>
                Results.Ok(RollbackPlanner.Planificar(
                    tipoApp, tieneSlots ?? false, planPremium ?? false)));

        // Slide 10 — alternativa: rollback vía feature flag.
        d.MapGet("/rollback/feature-flag", (string flag) =>
            Results.Ok(RollbackPlanner.PlanFeatureFlag(flag)));

        // Plan + checklist del entregable.
        d.MapPost("/plan", (EscenarioDeploy e, IDeploymentPlanner planner) =>
            Results.Ok(planner.Planificar(e)));
    }
}
