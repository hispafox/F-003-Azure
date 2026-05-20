using Deploy.Demo.Api.Deploy;

namespace Deploy.Demo.Api.Tests;

// CAPA 1 — estrategia por tipo de app (slides 3, 4, 5, 6, 7).
[Trait("Category", "Unit")]
public class Unit_StrategyTests
{
    [Theory]
    [InlineData(TipoApp.AppService, true, false, EstrategiaDeploy.SlotSwap)]
    [InlineData(TipoApp.AppService, false, false, EstrategiaDeploy.DirectDeploy)]
    [InlineData(TipoApp.Functions, false, true, EstrategiaDeploy.SlotSwap)]
    [InlineData(TipoApp.Functions, false, false, EstrategiaDeploy.DirectDeploy)]
    [InlineData(TipoApp.Msix, false, false, EstrategiaDeploy.AppInstaller)]
    [InlineData(TipoApp.Infra, false, false, EstrategiaDeploy.WhatIfApprove)]
    public void Recomendacion_Por_Tipo(
        TipoApp tipo, bool slots, bool premium, EstrategiaDeploy esperada)
    {
        var r = DeployStrategyAdvisor.Recomendar(
            new EscenarioDeploy(tipo, slots, premium));
        Assert.Equal(esperada, r.Estrategia);
    }

    [Fact]
    public void SlotSwap_Es_Zero_Downtime_Y_5_Segundos_Rollback()
    {
        var r = DeployStrategyAdvisor.Recomendar(
            new EscenarioDeploy(TipoApp.AppService, TieneSlots: true));
        Assert.Contains("Sin downtime", r.Downtime);
        Assert.Contains("5 segundos", r.RollbackTiempo);
    }

    [Fact]
    public void Infra_Mete_What_If_En_La_Razon()
    {
        var r = DeployStrategyAdvisor.Recomendar(
            new EscenarioDeploy(TipoApp.Infra));
        Assert.Contains("what-if", r.Razon, StringComparison.OrdinalIgnoreCase);
    }
}
