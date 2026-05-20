using Pipelines.Demo.Api.Pipelines;

namespace Pipelines.Demo.Api.Tests;

// CAPA 1 — advisor de triggers (slide 4).
[Trait("Category", "Unit")]
public class Unit_TriggerAdvisorTests
{
    [Fact]
    public void Ci_Principal_Incluye_Main_Y_Excluye_Docs()
    {
        var r = TriggerAdvisor.Recomendar(EscenarioTrigger.CiPrincipal);
        Assert.Contains("trigger:", r.Yaml);
        Assert.Contains("[main]", r.Yaml);
        Assert.Contains("docs", r.Yaml);
    }

    [Fact]
    public void Validacion_Pr_Empieza_Por_Pr_Branches()
    {
        var r = TriggerAdvisor.Recomendar(EscenarioTrigger.ValidacionPr);
        Assert.StartsWith("pr:", r.Yaml);
    }

    [Fact]
    public void Nightly_Tiene_Cron_Y_Always_True()
    {
        var r = TriggerAdvisor.Recomendar(EscenarioTrigger.NightlyBuild);
        Assert.Contains("cron:", r.Yaml);
        Assert.Contains("always: true", r.Yaml);
    }

    [Fact]
    public void Manual_Only_Es_Trigger_None()
        => Assert.Equal("trigger: none",
            TriggerAdvisor.Recomendar(EscenarioTrigger.ManualOnly).Yaml);

    [Fact]
    public void Recomendacion_Estandar_Incluye_Ci_Pr_Y_Nightly()
    {
        var r = TriggerAdvisor.RecomendacionEstandar();
        Assert.Equal(3, r.Count);
        Assert.Contains(r, x => x.Escenario == EscenarioTrigger.CiPrincipal);
        Assert.Contains(r, x => x.Escenario == EscenarioTrigger.ValidacionPr);
        Assert.Contains(r, x => x.Escenario == EscenarioTrigger.NightlyBuild);
    }
}
