using ClaudeCode.CasosUso.Demo.Api.CasosUso;

namespace ClaudeCode.CasosUso.Demo.Api.Endpoints;

public sealed record DescripcionRequest(string Descripcion);
public sealed record PromptRequest(string Prompt);
public sealed record PlanRequest(string Descripcion, string? PromptDelAlumno = null);

public static class CasosUsoEndpoints
{
    public static void MapCasosUso(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/casos");

        // Slides 2-16 — clasifica una descripción en uno de los 15 casos.
        g.MapPost("/clasificar", (DescripcionRequest r) =>
            Results.Ok(CaseClassifier.Clasificar(r.Descripcion)));

        // Slides 2-16 — template canónico para un caso concreto.
        g.MapGet("/template/{caso}", (CasoUso caso) =>
            Results.Ok(PromptTemplateBuilder.ParaCaso(caso)));

        // Slides 18-23 — evaluador de calidad del prompt del alumno.
        g.MapPost("/evaluar", (PromptRequest r) =>
            Results.Ok(PromptQualityEvaluator.Evaluar(r.Prompt)));

        // Plan: clasificar + template + (opcional) evaluar prompt + checklist.
        g.MapPost("/plan", (PlanRequest req, ICasosUsoPlanner planner) =>
            Results.Ok(planner.Planificar(req.Descripcion, req.PromptDelAlumno)));
    }
}
