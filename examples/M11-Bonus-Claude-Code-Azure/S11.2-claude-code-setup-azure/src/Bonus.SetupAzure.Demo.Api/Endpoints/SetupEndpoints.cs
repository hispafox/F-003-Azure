using Bonus.SetupAzure.Demo.Api.Setup;

namespace Bonus.SetupAzure.Demo.Api.Endpoints;

public sealed record ClaudeMdRequest(string Contenido);

public static class SetupEndpoints
{
    public static void MapSetup(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/setup");

        // Slide 4 — estructura recomendada del directorio `.claude/`.
        g.MapPost("/estructura", (EscenarioEquipo e) =>
            Results.Ok(CarpetaClaudeStructurer.Inventariar(e)));

        // Slide 7/9 — validador del settings.json.
        g.MapPost("/settings", (EscenarioSettings s) =>
            Results.Ok(SettingsPermissionsValidator.Validar(s)));

        // Slide 5/6 — evaluador de calidad del CLAUDE.md.
        g.MapPost("/claudemd", (ClaudeMdRequest r) =>
            Results.Ok(ClaudeMdQualityEvaluator.Evaluar(r.Contenido)));

        // Slide 16 — los 20 skills del Azure Skills Plugin.
        g.MapGet("/azure-skills",
            () => Results.Ok(SetupAzurePlanner.AzureSkillsSlide16));

        // Plan + checklist.
        g.MapPost("/plan", (PlanRequest req, ISetupAzurePlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
