using Deploy.Demo.Api.Deploy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Deploy.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void DeploymentPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IDeploymentPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IDeploymentPlanner>());

        var plan = planner.Planificar(new EscenarioDeploy(
            TipoApp.AppService, TieneSlots: true, Critico: true));

        Assert.Equal(EstrategiaDeploy.SlotSwap, plan.Estrategia.Estrategia);
        Assert.Contains("Swap", plan.Rollback.Metodo);
        Assert.NotEmpty(plan.Checklist);
    }
}
