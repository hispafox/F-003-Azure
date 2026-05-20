using AutoUpdate.Demo.Api.AutoUpdate;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoUpdate.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void AutoUpdatePlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IAutoUpdatePlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IAutoUpdatePlanner>());

        var plan = planner.Planificar(
            new EscenarioAutoUpdate(
                Canal: CanalDistribucion.Beta,
                ActualizacionCritica: true));

        Assert.Equal(CanalDistribucion.Beta, plan.Canal);
        Assert.Contains("msix-beta", plan.AppInstallerUri);
        Assert.True(plan.UpdateSettings.UpdateBlocksActivation);  // crítica
        Assert.Equal(0, plan.UpdateSettings.HoursBetweenUpdateChecks);
        Assert.Equal(new[] { 5, 25, 50, 100 }, plan.EtapasCanary);
        Assert.NotEmpty(plan.Checklist);
    }
}
