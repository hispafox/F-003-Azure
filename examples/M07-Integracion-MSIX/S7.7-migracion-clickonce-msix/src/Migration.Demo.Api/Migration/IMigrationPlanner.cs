namespace Migration.Demo.Api.Migration;

public sealed record EscenarioMigracion(
    ClickOnceManifest ClickOnce,
    IReadOnlyList<ComportamientoApp> Comportamientos,
    FaseMigracion FaseActual = FaseMigracion.Empaquetado);

public sealed record PlanMigracionClickOnceMsix(
    MappedAppxManifest Manifest,
    EvaluacionCompatibilidad Compatibilidad,
    FaseInfo Fase,
    IReadOnlyList<string> Checklist);

// Compone ClickOnceManifestMapper + MigrationCompatibilityCheck +
// MigrationRoadmap en un plan + checklist del entregable. Servicio
// inyectable (seam del test DI — lección M03-S3.4).
public interface IMigrationPlanner
{
    PlanMigracionClickOnceMsix Planificar(EscenarioMigracion escenario);
}

public sealed class MigrationPlanner : IMigrationPlanner
{
    public PlanMigracionClickOnceMsix Planificar(EscenarioMigracion e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var manifest = ClickOnceManifestMapper.Mapear(e.ClickOnce);
        var compat = MigrationCompatibilityCheck.Evaluar(e.Comportamientos);
        var fase = MigrationRoadmap.Info(e.FaseActual);

        return new PlanMigracionClickOnceMsix(
            manifest,
            compat,
            fase,
            // Slide 13/15/16/17/18 — checklist del entregable.
            Checklist:
            [
                "WPF/WinForms intacto: solo añadir el packaging project (slide 5)",
                "Coexistencia ClickOnce + MSIX durante la transición (slide 10/15)",
                "Migrar datos del usuario al primer arranque MSIX (slide 9/14)",
                "Comunicación a usuarios con calendario claro (slide 16/24)",
                "Plan de rollback: ClickOnce activo 4+ semanas (slide 18)",
                "Verificación post-migración con script PowerShell (slide 17)",
                "Pipeline CI/CD genera ambos paquetes durante coexistencia (slide 10)",
                "ClickOnce solo se desactiva tras 1 semana sin incidencias (slide 18)",
            ]);
    }
}
