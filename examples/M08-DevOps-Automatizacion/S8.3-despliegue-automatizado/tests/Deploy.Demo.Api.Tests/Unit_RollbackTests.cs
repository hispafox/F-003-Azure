using Deploy.Demo.Api.Deploy;

namespace Deploy.Demo.Api.Tests;

// CAPA 1 — plan de rollback (slides 8, 10).
[Trait("Category", "Unit")]
public class Unit_RollbackTests
{
    [Fact]
    public void AppService_Con_Slots_Es_Swap_Inverso_5s()
    {
        var p = RollbackPlanner.Planificar(TipoApp.AppService,
            tieneSlots: true, planPremium: false);
        Assert.Contains("Swap", p.Metodo);
        Assert.Contains("5 segundos", p.TiempoEstimado);
    }

    [Fact]
    public void AppService_Sin_Slots_Es_Redesplegar()
    {
        var p = RollbackPlanner.Planificar(TipoApp.AppService,
            tieneSlots: false, planPremium: false);
        Assert.Contains("Redesplegar", p.Metodo);
    }

    [Fact]
    public void Msix_Es_Publicar_Previa_Con_Build_Plus_1()
    {
        var p = RollbackPlanner.Planificar(TipoApp.Msix, false, false);
        Assert.Contains("build+1", p.Metodo);
        Assert.Contains(p.Pasos, x => x.Contains(".appinstaller"));
    }

    [Fact]
    public void Infra_Avisa_De_Recursos_Que_No_Se_Rollbackean()
    {
        var p = RollbackPlanner.Planificar(TipoApp.Infra, false, false);
        Assert.Contains(p.Pasos, x =>
            x.Contains("storage", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("restore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Feature_Flag_Es_Rollback_Sin_Redeploy_Slide_10()
    {
        var p = RollbackPlanner.PlanFeatureFlag("FEATURE_X");
        Assert.Contains("feature flag", p.Metodo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("segundos", p.TiempoEstimado);
        Assert.Contains(p.Pasos, x => x.Contains("FEATURE_X"));
    }
}
