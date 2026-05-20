namespace Devops.Repos.Demo.Api.Repos;

// Slides 5, 20 — políticas de rama recomendadas para `main`.
public enum BranchPolicy
{
    RequiredReviewers,         // slide 5 — mínimo 1 aprobador
    BuildExitoso,              // slide 5 — CI pasa antes de merge
    ResolucionDeComentarios,   // slide 5 — todos los comments resueltos
    NoPushDirecto,             // implícito en RequiredReviewers
    LimitarMergeTypes,         // squash merge recomendado (slide 4)
    LinkedWorkItems,           // slide 12 — PR vinculado a work item
}

public sealed record EvaluacionPolicies(
    IReadOnlyList<BranchPolicy> Faltantes,
    IReadOnlyList<BranchPolicy> Configuradas,
    bool Cumple);

// Slides 5, 20 — recomendaciones y check de las branch policies de
// `main`. Lógica pura.
public static class BranchPolicyAdvisor
{
    // Slide 5 — el conjunto mínimo NO NEGOCIABLE.
    public static IReadOnlyList<BranchPolicy> Minimas { get; } =
    [
        BranchPolicy.RequiredReviewers,
        BranchPolicy.BuildExitoso,
        BranchPolicy.ResolucionDeComentarios,
        BranchPolicy.NoPushDirecto,
    ];

    // Slide 12, 20 — recomendadas si trabajas con Boards y squash.
    public static IReadOnlyList<BranchPolicy> Recomendadas { get; } =
    [
        BranchPolicy.RequiredReviewers,
        BranchPolicy.BuildExitoso,
        BranchPolicy.ResolucionDeComentarios,
        BranchPolicy.NoPushDirecto,
        BranchPolicy.LimitarMergeTypes,
        BranchPolicy.LinkedWorkItems,
    ];

    // Compara `configuradas` contra las mínimas → reporta qué falta.
    public static EvaluacionPolicies Evaluar(
        IReadOnlyList<BranchPolicy> configuradas)
    {
        ArgumentNullException.ThrowIfNull(configuradas);
        var set = configuradas.ToHashSet();
        var faltantes = Minimas.Where(p => !set.Contains(p)).ToList();
        return new EvaluacionPolicies(
            Faltantes: faltantes,
            Configuradas: configuradas,
            Cumple: faltantes.Count == 0);
    }
}
