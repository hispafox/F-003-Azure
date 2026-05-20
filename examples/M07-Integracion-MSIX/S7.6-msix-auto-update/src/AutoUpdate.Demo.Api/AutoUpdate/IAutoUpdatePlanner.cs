namespace AutoUpdate.Demo.Api.AutoUpdate;

public sealed record EscenarioAutoUpdate(
    CanalDistribucion Canal = CanalDistribucion.Stable,
    bool ActualizacionCritica = false,        // slide 13 — bloquea activación
    bool DiferentialUpdate = true,             // slide 11 — mismo cert
    string BaseUrl = "https://stventasprod.blob.core.windows.net");

public sealed record PlanAutoUpdate(
    CanalDistribucion Canal,
    string AppInstallerUri,
    UpdateSettingsConfig UpdateSettings,
    IReadOnlyList<int> EtapasCanary,
    IReadOnlyList<string> Checklist);

// Compone AppInstallerBuilder + CanaryRolloutPolicy + UpdateVersionAdvisor
// en un plan + checklist del entregable. Servicio inyectable (seam del
// test DI — lección M03-S3.4).
public interface IAutoUpdatePlanner
{
    PlanAutoUpdate Planificar(EscenarioAutoUpdate escenario);
}

public sealed class AutoUpdatePlanner : IAutoUpdatePlanner
{
    public PlanAutoUpdate Planificar(EscenarioAutoUpdate e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var uri = CanaryRolloutPolicy.AppInstallerUri(e.Canal, e.BaseUrl);

        // Slide 13 — crítica → bloquea activación + check inmediato.
        var us = new UpdateSettingsConfig(
            HoursBetweenUpdateChecks: e.ActualizacionCritica ? 0 : 1,
            ShowPrompt: true,
            UpdateBlocksActivation: e.ActualizacionCritica,
            AutomaticBackgroundTask: true,
            ForceUpdateFromAnyVersion: true);

        return new PlanAutoUpdate(
            e.Canal,
            uri,
            us,
            EtapasCanary.Porcentajes,
            // Slide 24 anti-patterns + slides 11/12/15/17/21.
            Checklist:
            [
                "Staged rollout 5% → 25% → 50% → 100%, no big-bang (slide 20/24.2)",
                "Plan de rollback testado antes de la release (slide 8/24.3)",
                "Health checks post-update automáticos (slide 17/21)",
                "Telemetría de versión + crash reports en App Insights (slide 12/15/24.4)",
                "Diferential updates: nuevo .msix firmado con el mismo cert (slide 11)",
                "Mandatory updates con notice ≥ 24 h, idealmente 7 días (slide 13/24.5)",
                "Monitorizar 7-30 días post-deploy antes de cerrar el ticket (slide 24.9)",
                "Mensajes amistosos al usuario; permitir aplazar salvo críticas (slide 9/24.10)",
                "Watch AppInstaller deprecation roadmap 2026 — plan migración a winget (slide 18)",
            ]);
    }
}
