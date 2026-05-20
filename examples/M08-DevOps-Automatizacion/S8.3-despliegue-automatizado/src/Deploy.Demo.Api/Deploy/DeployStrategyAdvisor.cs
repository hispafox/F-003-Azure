namespace Deploy.Demo.Api.Deploy;

// Slide 3 — estrategias de despliegue.
public enum EstrategiaDeploy
{
    DirectDeploy,        // reinicio, alto riesgo
    SlotSwap,            // recomendado App Service / Functions Premium
    BlueGreen,           // dos entornos completos, swap DNS
    Canary,              // % progresivo
    RollingUpdate,       // K8s / AKS
    AppInstaller,        // MSIX (slide 6)
    WhatIfApprove,       // IaC Bicep (slide 7)
}

public enum TipoApp { AppService, Functions, Msix, Infra }

public sealed record EscenarioDeploy(
    TipoApp TipoApp,
    bool TieneSlots = false,
    bool PlanPremium = false,
    bool Critico = false);

public sealed record RecomendacionEstrategia(
    EstrategiaDeploy Estrategia,
    string Downtime,
    string RollbackTiempo,
    string Riesgo,
    string Razon);

// Slides 3, 4, 5, 6, 7, 8 — tabla de decisión por tipo de app.
public static class DeployStrategyAdvisor
{
    public static RecomendacionEstrategia Recomendar(EscenarioDeploy e)
    {
        ArgumentNullException.ThrowIfNull(e);

        return e.TipoApp switch
        {
            TipoApp.AppService when e.TieneSlots => new(
                EstrategiaDeploy.SlotSwap,
                "Sin downtime",
                "~5 segundos (swap inverso)",
                "Bajo",
                "App Service con slots → swap inverso para rollback (slide 3/8)."),

            TipoApp.AppService => new(
                EstrategiaDeploy.DirectDeploy,
                "Sí (reinicio)",
                "2-5 minutos (redesplegar)",
                "Alto",
                "App Service sin slots → considera habilitar staging (slide 3)."),

            TipoApp.Functions when e.PlanPremium => new(
                EstrategiaDeploy.SlotSwap,
                "Sin downtime",
                "~5 segundos",
                "Bajo",
                "Functions Premium soporta slots (slide 3/13)."),

            TipoApp.Functions => new(
                EstrategiaDeploy.DirectDeploy,
                "Pequeño (cold start)",
                "2-5 minutos",
                "Medio",
                "Consumption plan no soporta slots (slide 3)."),

            TipoApp.Msix => new(
                EstrategiaDeploy.AppInstaller,
                "Sin downtime",
                "1-24 h (auto-update)",
                "Bajo",
                "MSIX: publicar nueva versión + AppInstaller actualiza " +
                "a los usuarios (slide 6)."),

            TipoApp.Infra => new(
                EstrategiaDeploy.WhatIfApprove,
                "Depende del recurso",
                "Re-deploy del estado anterior",
                "Variable",
                "Bicep: what-if obligatorio antes de aprobar (slide 7). " +
                "Si ves 'Delete: ...' algo va mal antes de ejecutar."),

            _ => throw new ArgumentOutOfRangeException(nameof(e)),
        };
    }
}
