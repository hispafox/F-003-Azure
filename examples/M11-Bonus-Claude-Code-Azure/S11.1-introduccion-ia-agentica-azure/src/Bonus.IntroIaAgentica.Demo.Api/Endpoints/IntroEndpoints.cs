using Bonus.IntroIaAgentica.Demo.Api.Intro;

namespace Bonus.IntroIaAgentica.Demo.Api.Endpoints;

public sealed record HerramientaRequest(string Descripcion);

public static class IntroEndpoints
{
    public static void MapIntro(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/intro");

        // Slide 3 — clasificador por generación de IA.
        g.MapPost("/generacion", (HerramientaRequest r) =>
            Results.Ok(GeneracionIaClassifier.Clasificar(r.Descripcion)));

        // Slide 9 — tabla canónica Claude Code vs Cowork.
        g.MapGet("/comparativa", () => Results.Ok(CcVsCoworkRecommender.Tabla));

        // Slide 9 — recomendador por escenario.
        g.MapPost("/recomendar", (EscenarioUso e) =>
            Results.Ok(CcVsCoworkRecommender.Recomendar(e)));

        // Slides 10 + 18 — evaluador del nivel de madurez.
        g.MapPost("/nivel", (EscenarioEquipo e) =>
            Results.Ok(NivelUsoEvaluator.Evaluar(e)));

        // Slide 7 — objetivos del módulo M11.
        g.MapGet("/objetivos",
            () => Results.Ok(IntroIaAgenticaPlanner.ObjetivosM11Slide7));

        // Plan + checklist.
        g.MapPost("/plan", (PlanRequest req, IIntroIaAgenticaPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
