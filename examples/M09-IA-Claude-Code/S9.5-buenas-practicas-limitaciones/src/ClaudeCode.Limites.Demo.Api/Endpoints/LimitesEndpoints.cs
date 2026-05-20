using ClaudeCode.Limites.Demo.Api.Limites;

namespace ClaudeCode.Limites.Demo.Api.Endpoints;

public sealed record UsoRequest(string Descripcion);
public sealed record PromptRequest(string Prompt);
public sealed record PlanRequest(
    string? DescripcionUso = null,
    string? PromptDelAlumno = null,
    TipoTareaIa? TipoTarea = null);

public static class LimitesEndpoints
{
    public static void MapLimites(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/limites");

        // Slide 2 — las 7 reglas de oro.
        g.MapGet("/reglas", () => Results.Ok(LimitesPlanner.ReglasDeOroSlide2));

        // Slide 13 — detector de anti-patterns en la descripción de uso.
        g.MapPost("/antipatterns", (UsoRequest r) =>
            Results.Ok(AntiPatternDetector.Detectar(r.Descripcion)));

        // Slide 12 — validador del template de 7 secciones.
        g.MapPost("/estructura", (PromptRequest r) =>
            Results.Ok(PromptStructureValidator.Validar(r.Prompt)));

        // Slide 5 — clasificador acelera vs frena.
        g.MapGet("/acelera-o-frena/{tipo}", (TipoTareaIa tipo) =>
            Results.Ok(AceleraOFrenaClassifier.Clasificar(tipo)));

        // Plan + checklist.
        g.MapPost("/plan", (PlanRequest req, ILimitesPlanner planner) =>
            Results.Ok(planner.Planificar(req.DescripcionUso, req.PromptDelAlumno, req.TipoTarea)));
    }
}
