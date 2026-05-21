using Practica.MiniNotas.Demo.Api.MiniNotas;

namespace Practica.MiniNotas.Demo.Api.Endpoints;

public static class MiniNotasEndpoints
{
    public static void MapMiniNotas(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/mininotas");

        // Slide 3 — preflight ligero.
        g.MapPost("/preflight", (EscenarioPreflight e) =>
            Results.Ok(MiniNotasPreflight.Comprobar(e)));

        // Slides 4-14 — evaluador de cada paso.
        g.MapPost("/paso", (EvidenciaPaso e) =>
            Results.Ok(PasoChecker.Evaluar(e)));

        // Slide 2 — comparador de alcance.
        g.MapPost("/alcance", (EscenarioObjetivo o) =>
            Results.Ok(AlcanceComparator.Comparar(o)));

        // Slide 2 — camino de extensión hacia el proyecto integrador.
        g.MapGet("/camino-s101",
            () => Results.Ok(PracticaMiniNotasPlanner.CaminoHaciaS101Slide2));

        // Plan + checklist.
        g.MapPost("/plan", (PlanRequest req, IPracticaMiniNotasPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
