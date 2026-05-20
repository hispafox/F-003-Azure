namespace Devops.Repos.Demo.Api.Repos;

public sealed record PlanRepoBoards(
    EstrategiaRepo EstrategiaRecomendada,
    IReadOnlyList<string> RazonesEstrategia,
    IReadOnlyList<BranchPolicy> PoliciesMinimas,
    IReadOnlyList<BranchPolicy> PoliciesRecomendadas,
    IReadOnlyList<string> Checklist);

// Compone RepoStrategyAdvisor + BranchPolicyAdvisor +
// ConventionalCommitParser en el plan + checklist del entregable.
// Servicio inyectable (seam del test DI — lección M03-S3.4).
public interface IRepoBoardsPlanner
{
    PlanRepoBoards Planificar(EscenarioEquipo equipo);
}

public sealed class RepoBoardsPlanner : IRepoBoardsPlanner
{
    public PlanRepoBoards Planificar(EscenarioEquipo equipo)
    {
        ArgumentNullException.ThrowIfNull(equipo);
        var rec = RepoStrategyAdvisor.Recomendar(equipo);

        return new PlanRepoBoards(
            rec.Estrategia,
            rec.Razones,
            BranchPolicyAdvisor.Minimas,
            BranchPolicyAdvisor.Recomendadas,
            // Slides 4, 6, 7, 9, 13, 15 — checklist del entregable.
            Checklist:
            [
                "Trunk-based: feature branches de 2-5 días + squash merge a main (slide 4)",
                "Branch policies en main (RequiredReviewers ≥1, build OK, comments resueltos) (slide 5)",
                "Pull Request obligatorio + 1 reviewer mínimo (slide 6)",
                "Conventional Commits: feat/fix/docs/refactor/test/chore/perf/ci (slide 7)",
                "Vincular commits/PRs a work items con #NNNN o 'Fixes #NNNN' (slide 12)",
                "Jerarquía Epic → Feature → User Story → Task / Bug (slide 9)",
                "Sprints de 2 semanas con velocity tras 3-4 sprints (slide 10)",
                "Feed de Artifacts privado para shared libraries (slide 13)",
                "Seguridad: PAT con expiración corta + permissions mínimas (slide 15)",
            ]);
    }
}
