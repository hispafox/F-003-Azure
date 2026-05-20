using Practica.CcMcp.Demo.Api.Practica;

namespace Practica.CcMcp.Demo.Api.Endpoints;

public sealed record ComparativaRequest(string Vago, string Medio, string Detallado);

public static class PracticaEndpoints
{
    public static void MapPractica(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/practica");

        // Slide 2/8 — preflight.
        g.MapPost("/preflight", (EscenarioPreflight e) =>
            Results.Ok(PracticaPreflight.Comprobar(e)));

        // Slides 3-7, 11-13 — evaluar un ejercicio concreto.
        g.MapPost("/ejercicio", (EvidenciaEjercicio e) =>
            Results.Ok(EjercicioEvaluator.Evaluar(e)));

        // Slide 12 — comparativa de prompts (3 niveles).
        g.MapPost("/comparativa", (ComparativaRequest r) =>
            Results.Ok(PromptComparison.Comparar(r.Vago, r.Medio, r.Detallado)));

        // Slide 8 — plan + checklist de la práctica.
        g.MapPost("/plan", (EvaluacionRequest req, IPracticaCcMcpPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
