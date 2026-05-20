namespace Deploy.Demo.Api.Deploy;

public sealed record PlanRollback(
    string Metodo,
    string TiempoEstimado,
    IReadOnlyList<string> Pasos);

// Slide 8 — plan de rollback por tipo de app + slide 10 (feature
// flags como alternativa que evita el redeploy).
public static class RollbackPlanner
{
    public static PlanRollback Planificar(TipoApp tipo, bool tieneSlots, bool planPremium) =>
        tipo switch
        {
            TipoApp.AppService when tieneSlots => new(
                "Swap inverso",
                "~5 segundos",
                [
                    "Verificar que el slot `staging` aún tiene la versión anterior",
                    "Ejecutar Swap Slots con SourceSlot=staging",
                    "Comprobar health en el slot de producción tras el swap",
                ]),

            TipoApp.AppService => new(
                "Redesplegar versión anterior",
                "2-5 minutos",
                [
                    "Localizar el artifact de la versión anterior (Pipeline runs)",
                    "Re-ejecutar el job de deploy con ese artifact",
                    "Verificar health post-deploy",
                ]),

            TipoApp.Functions when planPremium => new(
                "Swap inverso (Premium)",
                "~5 segundos",
                [
                    "Swap del slot `staging` a producción",
                    "Verificar que el contador de Functions sigue siendo el esperado",
                ]),

            TipoApp.Functions => new(
                "Redesplegar Function App",
                "2-5 minutos (con cold start)",
                [
                    "Re-deploy del zip anterior",
                    "Verificar `az functionapp function list` cuenta",
                ]),

            TipoApp.Msix => new(
                "Publicar previa con build+1",
                "1-24 h (depende del intervalo de AppInstaller)",
                [
                    "Mantener el .msix de la versión BUENA disponible",
                    "Re-publicar con un build number mayor que el malo",
                    "Actualizar el .appinstaller para apuntar a la nueva versión",
                    "Los usuarios reciben la 'nueva' versión = código bueno",
                ]),

            TipoApp.Infra => new(
                "Re-deploy del estado anterior",
                "Depende de los recursos (puede requerir manual)",
                [
                    "Recuperar el manifest Bicep de la versión anterior",
                    "Ejecutar `az deployment group what-if` para previsualizar",
                    "Aprobar y re-aplicar",
                    "Algunos recursos (storage, dbs) no se rollbackean — restore data",
                ]),

            _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
        };

    // Slide 10 — alternativa SIN redeploy: feature flag.
    public static PlanRollback PlanFeatureFlag(string flagName) =>
        new("Desactivar feature flag",
            "~segundos",
            [
                $"App Settings → poner {flagName}=false",
                "Reiniciar Workers / esperar a la siguiente lectura (~30s)",
                "Sin redeploy ni swap; el código sigue desplegado",
            ]);
}
