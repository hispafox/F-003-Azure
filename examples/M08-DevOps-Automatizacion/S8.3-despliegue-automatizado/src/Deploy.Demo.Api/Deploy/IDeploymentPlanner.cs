namespace Deploy.Demo.Api.Deploy;

public sealed record PlanDeploy(
    RecomendacionEstrategia Estrategia,
    PlanRollback Rollback,
    IReadOnlyList<string> Checklist);

// Compone DeployStrategyAdvisor + RollbackPlanner en el plan +
// checklist del entregable. Servicio inyectable (seam del test DI —
// lección M03-S3.4).
public interface IDeploymentPlanner
{
    PlanDeploy Planificar(EscenarioDeploy escenario);
}

public sealed class DeploymentPlanner : IDeploymentPlanner
{
    public PlanDeploy Planificar(EscenarioDeploy e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var estrategia = DeployStrategyAdvisor.Recomendar(e);
        var rollback = RollbackPlanner.Planificar(e.TipoApp, e.TieneSlots, e.PlanPremium);

        return new PlanDeploy(
            estrategia,
            rollback,
            // Slides 4, 7, 9, 10, 14, 15 — checklist del entregable.
            Checklist:
            [
                "Deploy a slot `staging` antes del swap a producción (slide 4)",
                "Smoke test del slot `staging` antes del swap (slide 4/9)",
                "Aprobación manual del swap a producción (slide 4/8)",
                "Health check post-deploy con retry (5 intentos x 10s) (slide 9)",
                "Auto-rollback si el smoke test falla (slide 9 `condition: failed()`)",
                "Bicep: `what-if` obligatorio antes de aplicar (slide 7)",
                "Sticky settings: connection strings no se swap (slide 14)",
                "Warmup del slot tras el deploy para evitar cold start (slide 15)",
                "Feature flags como alternativa al rollback completo (slide 10)",
                "Artifact retention 90 días para rollback rápido (slide 17)",
            ]);
    }
}
