using ClaudeCode.Intro.Demo.Api.ClaudeCode;

namespace ClaudeCode.Intro.Demo.Api.Endpoints;

public sealed record PlanRequest(
    EscenarioElegirHerramienta Herramienta,
    EscenarioEquipo Equipo,
    EscenarioTarea? TareaConcreta = null);

public static class ClaudeCodeEndpoints
{
    public static void MapClaudeCode(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/cc");

        // Slide 5 — comparativa Claude Code vs GitHub Copilot.
        g.MapGet("/comparativa", () => Results.Ok(ToolComparison.Tabla));

        // Slide 5 — recomendación por escenario.
        g.MapPost("/recomendar", (EscenarioElegirHerramienta e) =>
            Results.Ok(ToolComparison.Recomendar(e)));

        // Slides 4/7-10/12/15/16/18/19/20 — recomendador de modo +
        // features complementarias.
        g.MapPost("/feature", (EscenarioTarea t) =>
            Results.Ok(FeatureRecommender.Recomendar(t)));

        // Slides 6/11/13/19 — settings.json recomendado del equipo.
        g.MapPost("/settings", (EscenarioEquipo eq) =>
            Results.Ok(ProjectConfigBuilder.Construir(eq)));

        // Plan + checklist de onboarding.
        g.MapPost("/plan", (PlanRequest req, IClaudeCodePlanner planner) =>
            Results.Ok(planner.Planificar(req.Herramienta, req.Equipo, req.TareaConcreta)));
    }
}
