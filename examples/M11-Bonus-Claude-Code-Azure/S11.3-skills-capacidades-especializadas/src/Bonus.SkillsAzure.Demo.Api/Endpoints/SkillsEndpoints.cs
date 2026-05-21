using Bonus.SkillsAzure.Demo.Api.Skills;

namespace Bonus.SkillsAzure.Demo.Api.Endpoints;

public sealed record SkillMdRequest(string SkillMd);
public sealed record DescriptionRequest(string Description);

public static class SkillsEndpoints
{
    public static void MapSkills(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/skills");

        // Slide 6 — validador del frontmatter del SKILL.md.
        g.MapPost("/frontmatter", (SkillMdRequest r) =>
            Results.Ok(SkillFrontmatterValidator.Validar(r.SkillMd)));

        // Slide 16/24 — scorer de la `description`.
        g.MapPost("/description", (DescriptionRequest r) =>
            Results.Ok(SkillDescriptionScorer.Evaluar(r.Description)));

        // Slide 17 — detector de anti-patrones.
        g.MapPost("/antipatterns", (SkillMdRequest r) =>
            Results.Ok(SkillAntiPatternDetector.Detectar(r.SkillMd)));

        // Slide 18 — los skills oficiales de Microsoft.
        g.MapGet("/microsoft",
            () => Results.Ok(SkillLibraryPlanner.SkillsMicrosoftSlide18));

        // Plan + roadmap + checklist.
        g.MapPost("/plan", (PlanRequest req, ISkillLibraryPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
