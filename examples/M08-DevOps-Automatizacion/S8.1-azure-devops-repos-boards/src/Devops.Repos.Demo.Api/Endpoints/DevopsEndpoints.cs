using Devops.Repos.Demo.Api.Repos;

namespace Devops.Repos.Demo.Api.Endpoints;

public sealed record CommitRequest(string Mensaje);
public sealed record PoliciesRequest(List<BranchPolicy> Configuradas);

public static class DevopsEndpoints
{
    public static void MapDevops(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var d = app.MapGroup("/devops");

        // Slide 7 — parsea un mensaje de commit Conventional.
        d.MapPost("/commit/parsear", (CommitRequest r) =>
            Results.Ok(ConventionalCommitParser.Parsear(r.Mensaje)));

        // Slide 7 — listado de tipos válidos.
        d.MapGet("/commit/tipos", () =>
            Results.Ok(TiposCommit.Validos.OrderBy(x => x)));

        // Slide 5 — branch policies recomendadas.
        d.MapGet("/branch-policy/minimas", () =>
            Results.Ok(BranchPolicyAdvisor.Minimas));
        d.MapGet("/branch-policy/recomendadas", () =>
            Results.Ok(BranchPolicyAdvisor.Recomendadas));

        // Slide 5/20 — ¿cumplen las policies configuradas?
        d.MapPost("/branch-policy/evaluar", (PoliciesRequest r) =>
            Results.Ok(BranchPolicyAdvisor.Evaluar(r.Configuradas)));

        // Slide 3 — monorepo vs multi-repo.
        d.MapPost("/repo/estrategia", (EscenarioEquipo e) =>
            Results.Ok(RepoStrategyAdvisor.Recomendar(e)));

        // Plan + checklist del entregable.
        d.MapPost("/plan", (EscenarioEquipo e, IRepoBoardsPlanner planner) =>
            Results.Ok(planner.Planificar(e)));
    }
}
