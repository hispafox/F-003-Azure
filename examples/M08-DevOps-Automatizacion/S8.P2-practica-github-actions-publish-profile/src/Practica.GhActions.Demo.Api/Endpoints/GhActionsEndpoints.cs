using Practica.GhActions.Demo.Api.GhActions;

namespace Practica.GhActions.Demo.Api.Endpoints;

public sealed record PublishProfileRequest(string Xml);

public sealed record PlanRequest(
    string? PublishProfileXml,
    OpcionesWorkflow Opciones,
    EscenarioAuth Escenario);

public static class GhActionsEndpoints
{
    public static void MapGhActions(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/ghactions");

        // Slide 7/17 — parser del publish profile XML.
        g.MapPost("/profile/parsear", (PublishProfileRequest r) =>
            Results.Ok(PublishProfileParser.Parsear(r.Xml)));

        // Slides 9/14/15/18 — esqueleto del workflow GitHub Actions.
        g.MapPost("/workflow", (OpcionesWorkflow o) =>
            Results.Ok(WorkflowBuilder.Construir(o)));

        // Slide 13/18 — recomendador Publish Profile vs OIDC.
        g.MapPost("/auth/recomendar", (EscenarioAuth e) =>
            Results.Ok(MetodoAuthRecomendador.Recomendar(e)));

        // Slide 2/16/18 — plan + checklist de la práctica.
        g.MapPost("/plan", (PlanRequest req, IPracticaGhActionsPlanner planner) =>
            Results.Ok(planner.Planificar(req.PublishProfileXml, req.Opciones, req.Escenario)));
    }
}
